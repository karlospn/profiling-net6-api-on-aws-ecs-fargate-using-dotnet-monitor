using System.Collections.Generic;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.ElasticLoadBalancingV2;
using Constructs;
using Protocol = Amazon.CDK.AWS.EC2.Protocol;

namespace FargateCdkStack.Constructs
{
    public class EcsServiceConstruct : Construct
    {
        public FargateService FargateService { get; }

        public EcsServiceConstruct(Construct scope,
            string id,
            Vpc vpc,
            Cluster cluster,
            ApplicationLoadBalancer pubAlb,
            ApplicationLoadBalancer monAlb)
            : base(scope, id)
        {
            var task = CreateTaskDefinition(vpc);
            FargateService = CreateEcsService(vpc, cluster, pubAlb, monAlb, task);
        }

        private FargateTaskDefinition CreateTaskDefinition(Vpc vpc)
        {
            var task = new FargateTaskDefinition(this,
                "task-definition-ecs-profiling-dotnet-demo",
                new FargateTaskDefinitionProps
                {
                    Cpu = 1024,
                    Family = "task-definition-ecs-profiling-dotnet-demo",
                    MemoryLimitMiB = 2048
                });

            task.AddVolume(new Amazon.CDK.AWS.ECS.Volume
            {
                Name = "diagnostics"
            });

            task.AddVolume(new Amazon.CDK.AWS.ECS.Volume
            {
                Name = "dumps"
            });

            var linuxParams = new LinuxParameters(this, "sys-ptrace-linux-params");
            linuxParams.AddCapabilities(Capability.SYS_PTRACE);

            task.AddContainer("container-app",
                new ContainerDefinitionOptions
                {
                    Cpu = 512,
                    MemoryLimitMiB = 1024,
                    Image = ContainerImage.FromAsset("../src/Profiling.Api"),
                    LinuxParameters = linuxParams,
                    Environment = new Dictionary<string, string>
                    {
                        {"DOTNET_DiagnosticPorts","/diag/port,nosuspend,connect"}
                    },
                    Logging = LogDriver.AwsLogs(new AwsLogDriverProps
                    {
                        StreamPrefix = "ecs"
                    }),
                    PortMappings = new IPortMapping[]
                    {
                        new PortMapping
                        {
                            ContainerPort = 80
                        }
                    }
                }).AddMountPoints(new MountPoint
            {
                ContainerPath = "/diag",
                SourceVolume = "diagnostics"
            }, new MountPoint
            {
                ContainerPath = "/dumps",
                SourceVolume = "dumps",
                ReadOnly = false
            });

            task.AddContainer("dotnet-monitor",
                new ContainerDefinitionOptions
                {
                    Cpu = 256,
                    MemoryLimitMiB = 512,
                    Image = ContainerImage.FromRegistry("mcr.microsoft.com/dotnet/monitor:6"),
                    Environment = new Dictionary<string, string>
                    {
                        { "DOTNETMONITOR_DiagnosticPort__ConnectionMode", "Listen" },
                        { "DOTNETMONITOR_DiagnosticPort__EndpointName", "/diag/port" },
                        { "DOTNETMONITOR_Urls", "http://+:52323" },
                        { "DOTNETMONITOR_Storage__DumpTempFolder", "/dumps"}
                    },
                    Command = new[] { "--no-auth" },
                    PortMappings = new IPortMapping[]
                    {
                        new PortMapping
                        {
                            ContainerPort = 52323
                        }
                    }

                }).AddMountPoints(new MountPoint
            {
                ContainerPath = "/diag",
                SourceVolume = "diagnostics"
            }, new MountPoint
            {
                ContainerPath = "/dumps",
                SourceVolume = "dumps",
                ReadOnly = false
            });

            return task;
        }

        private FargateService CreateEcsService(Vpc vpc,
            Cluster cluster,
            ApplicationLoadBalancer pubAlb,
            ApplicationLoadBalancer monAlb,
            FargateTaskDefinition task)
        {
            
            var sg = new SecurityGroup(this,
                "scg-svc-ecs-profiling-dotnet-demo",
                new SecurityGroupProps
                {
                    SecurityGroupName = "scg-svc-ecs-profiling-dotnet-demo",
                    Description = "Allow traffic from ALB to app",
                    AllowAllOutbound = true,
                    Vpc = vpc
                });

            sg.Connections.AllowFrom(pubAlb.Connections, new Port(new PortProps
            {
                FromPort = 80,
                ToPort = 80,
                Protocol = Protocol.TCP,
                StringRepresentation = string.Empty
            }));

            sg.Connections.AllowFrom(monAlb.Connections, new Port(new PortProps
            {
                FromPort = 52323,
                ToPort = 52323,
                Protocol = Protocol.TCP,
                StringRepresentation = string.Empty
            }));


            var service = new FargateService(this,
                "service-ecs-profiling-dotnet-demo",
                new FargateServiceProps
                {
                    TaskDefinition = task,
                    Cluster = cluster,
                    DesiredCount = 1,
                    MinHealthyPercent = 100,
                    MaxHealthyPercent = 200,
                    AssignPublicIp = true,
                    VpcSubnets = new SubnetSelection
                    {
                        Subnets = vpc.PublicSubnets
                    },
                    SecurityGroups = new ISecurityGroup[] { sg },
                    ServiceName = "service-ecs-profiling-dotnet-demo",
                });

            return service;
        }
    }
}
