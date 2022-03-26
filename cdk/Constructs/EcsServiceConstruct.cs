using System.Collections.Generic;
using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.ElasticLoadBalancingV2;
using Constructs;
using HealthCheck = Amazon.CDK.AWS.ElasticLoadBalancingV2.HealthCheck;
using Protocol = Amazon.CDK.AWS.EC2.Protocol;

namespace FargateCdkStack.Constructs
{
    public class EcsServiceConstruct : Construct
    {
        private FargateService FargateService { get; set; }

        public EcsServiceConstruct(Construct scope,
            string id,
            Vpc vpc,
            Cluster cluster,
            ApplicationLoadBalancer alb,
            ApplicationListener httpListener,
            ApplicationListener monitorListener)
            : base(scope, id)
        {
            var task = CreateTaskDefinition();
            FargateService = CreateEcsService(vpc, cluster, alb, task);
            CreateTargetGroup(vpc, httpListener, monitorListener, FargateService);
        }

        private FargateTaskDefinition CreateTaskDefinition()
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
                Name = "profiling"
            });

            task.AddContainer("container-app",
                new ContainerDefinitionOptions
                {
                    Cpu = 512,
                    MemoryLimitMiB = 1024,
                    Image = ContainerImage.FromAsset("../src/Profiling.Api"),
                    Environment = new Dictionary<string, string>
                    {
                        {"DOTNET_DiagnosticPorts","/tmp/dotnet-monitor-pipe,nosuspend,connect"}
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
                ContainerPath = "/tmp",
                SourceVolume = "profiling"
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
                        { "DOTNETMONITOR_DiagnosticPort__EndpointName", "/tmp/dotnet-monitor-pipe" },
                        { "DOTNETMONITOR_Urls", "http://+:52323" }

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
                ContainerPath = "/tmp",
                SourceVolume = "profiling"
            });

            return task;
        }

        private FargateService CreateEcsService(Vpc vpc,
            Cluster cluster,
            ApplicationLoadBalancer alb,
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

            sg.Connections.AllowFrom(alb.Connections, new Port(new PortProps
            {
                FromPort = 80,
                ToPort = 80,
                Protocol = Protocol.TCP,
                StringRepresentation = string.Empty
            }));

            sg.Connections.AllowFrom(alb.Connections, new Port(new PortProps
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

        private void CreateTargetGroup(Vpc vpc,
            ApplicationListener httpListener,
            ApplicationListener monitorListener,
            FargateService service)
        {


            var target1 = service.LoadBalancerTarget(new LoadBalancerTargetOptions
            {
                ContainerPort = 80,
                Protocol = Amazon.CDK.AWS.ECS.Protocol.TCP,
                ContainerName = "container-app"
            });

            var targetGroup = new ApplicationTargetGroup(this,
                "tg-app-ecs-profiling-dotnet-demo",
                new ApplicationTargetGroupProps
                {
                    TargetGroupName = "tg-app-profiling-dotnet-demo",
                    Vpc = vpc,
                    TargetType = TargetType.IP,
                    ProtocolVersion = ApplicationProtocolVersion.HTTP1,
                    
                    HealthCheck = new HealthCheck
                    {
                        Protocol = Amazon.CDK.AWS.ElasticLoadBalancingV2.Protocol.HTTP,
                        HealthyThresholdCount = 3,
                        Path = "/health",
                        Port = "80",
                        Interval = Duration.Millis(10000),
                        Timeout = Duration.Millis(8000),
                        UnhealthyThresholdCount = 10,
                        HealthyHttpCodes = "200"
                    },
                    Port = 80,
                    Targets = new IApplicationLoadBalancerTarget[] { target1 }
                });


            var target2 = service.LoadBalancerTarget(new LoadBalancerTargetOptions
            {
                ContainerPort = 52323,
                Protocol = Amazon.CDK.AWS.ECS.Protocol.TCP,
                ContainerName = "dotnet-monitor"
            });

            var monitorGroup = new ApplicationTargetGroup(this,
                "tg-monitor-ecs-profiling-dotnet-demo",
                new ApplicationTargetGroupProps
                {
                    TargetGroupName = "tg-mon-profiling-dotnet-demo",
                    Vpc = vpc,
                    TargetType = TargetType.IP,
                    ProtocolVersion = ApplicationProtocolVersion.HTTP1,
                    Protocol = ApplicationProtocol.HTTP,
                    HealthCheck = new HealthCheck
                    {
                        Protocol = Amazon.CDK.AWS.ElasticLoadBalancingV2.Protocol.HTTP,
                        HealthyThresholdCount = 3,
                        Path = "/info",
                        Port = "52323",
                        Interval = Duration.Millis(10000),
                        Timeout = Duration.Millis(8000),
                        UnhealthyThresholdCount = 10,
                        HealthyHttpCodes = "200"
                    },
                    Port = 52323,
                    Targets = new IApplicationLoadBalancerTarget[] { target2 }
                });

            httpListener.AddTargetGroups(
                "app-listener",
                new AddApplicationTargetGroupsProps
                {
                    TargetGroups = new IApplicationTargetGroup[] { targetGroup }
                });
                
            monitorListener.AddTargetGroups(
                "monitor-listener",
                new AddApplicationTargetGroupsProps
                {
                    TargetGroups = new IApplicationTargetGroup[] { monitorGroup }
                });

        }

    }
}
