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
        private ApplicationTargetGroup TargetGroup { get; set; }
        private FargateService FargateService { get; set; }

        public EcsServiceConstruct(Construct scope,
            string id,
            Vpc vpc,
            Cluster cluster,
            ApplicationLoadBalancer alb,
            ApplicationListener albListener)
            : base(scope, id)
        {
            var task = CreateTaskDefinition();
            FargateService = CreateEcsService(vpc, cluster, alb, task);
            CreateTargetGroup(vpc, albListener, FargateService);
        }

        private FargateTaskDefinition CreateTaskDefinition()
        {

            var task = new FargateTaskDefinition(this,
                "task-definition-ecs-profiling-dotnet-demo",
                new FargateTaskDefinitionProps
                {
                    Cpu = 512,
                    Family = "task-definition-ecs-profiling-dotnet-demo",
                    MemoryLimitMiB = 1024
                });

            task.AddContainer("container-app",
                new ContainerDefinitionOptions
                {
                    Cpu = 512,
                    MemoryLimitMiB = 1024,
                    Image = ContainerImage.FromAsset("../src/Profiling.Api"),
                    Logging = LogDriver.AwsLogs(new AwsLogDriverProps
                    {
                        StreamPrefix = "ecs"
                    }),
                }).AddPortMappings(new PortMapping
                {
                    ContainerPort = 80,
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
                    ServiceName = "service-ecs-profiling-dotnet-demo"
                });
            return service;
        }

        private void CreateTargetGroup(Vpc vpc, 
            ApplicationListener albListener,
            FargateService service)
        {
            TargetGroup = new ApplicationTargetGroup(this,
                "tg-ecs-profiling-dotnet-demo",
                new ApplicationTargetGroupProps
                {
                    TargetGroupName = "tg-ecs-profiling-dotnet-demo",
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
                    Targets = new IApplicationLoadBalancerTarget[] { service }
                });


            albListener.AddTargetGroups(
                "listener",
                new AddApplicationTargetGroupsProps
                {
                    TargetGroups = new IApplicationTargetGroup[] { TargetGroup },
                    Conditions = new[] { ListenerCondition.PathPatterns(new[] { "/app/*" }) },
                    Priority = 1
                });

        }

    }
}
