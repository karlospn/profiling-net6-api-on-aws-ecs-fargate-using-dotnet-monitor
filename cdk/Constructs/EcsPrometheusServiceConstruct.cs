using System.Collections.Generic;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.ElasticLoadBalancingV2;
using Constructs;
using Protocol = Amazon.CDK.AWS.EC2.Protocol;

namespace FargateCdkStack.Constructs
{
    public class EcsPrometheusServiceConstruct : Construct
    {
        public FargateService FargateService { get; }

        public EcsPrometheusServiceConstruct(Construct scope,
            string id,
            Vpc vpc,
            Cluster cluster,
            ApplicationLoadBalancer monAlb)
            : base(scope, id)
        {
            var task = CreateTaskDefinition();
            FargateService = CreateEcsService(vpc, cluster, monAlb, task);
        }

        private FargateTaskDefinition CreateTaskDefinition()
        {
            var task = new FargateTaskDefinition(this,
                "task-definition-prometheus",
                new FargateTaskDefinitionProps
                {
                    Cpu = 1024,
                    Family = "task-definition-prometheus",
                    MemoryLimitMiB = 2048
                });

            task.AddContainer("prometheus-app",
                new ContainerDefinitionOptions
                {
                    Cpu = 1024,
                    MemoryLimitMiB = 2048,
                    Image = ContainerImage.FromAsset("../prometheus"),
                    Logging = LogDriver.AwsLogs(new AwsLogDriverProps
                    {
                        StreamPrefix = "ecs"
                    }),
                    PortMappings = new IPortMapping[]
                    {
                        new PortMapping
                        {
                            ContainerPort = 9090
                        }
                    }
                });

            return task;
        }

        private FargateService CreateEcsService(Vpc vpc,
            Cluster cluster,
            ApplicationLoadBalancer monAlb,
            FargateTaskDefinition task)
        {
            
            var sg = new SecurityGroup(this,
                "scg-prometheus",
                new SecurityGroupProps
                {
                    SecurityGroupName = "scg-prometheus",
                    Description = "Allow traffic from ALB to prometheus",
                    AllowAllOutbound = true,
                    Vpc = vpc
                });


            sg.Connections.AllowFrom(monAlb.Connections, new Port(new PortProps
            {
                FromPort = 9090,
                ToPort = 9090,
                Protocol = Protocol.TCP,
                StringRepresentation = string.Empty
            }));


            var service = new FargateService(this,
                "service-prometheus",
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
                    ServiceName = "service-prometheus",
                });

            return service;
        }
    }
}
