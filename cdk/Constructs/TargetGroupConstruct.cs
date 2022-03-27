using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.ElasticLoadBalancingV2;
using Constructs;
using HealthCheck = Amazon.CDK.AWS.ElasticLoadBalancingV2.HealthCheck;

namespace FargateCdkStack.Constructs
{
    public class TargetGroupConstruct : Construct
    {
        public TargetGroupConstruct(Construct scope, 
            string id,
            Vpc vpc,
            ApplicationLoadBalancer pubAlb,
            ApplicationLoadBalancer monAlb,
            FargateService service) 
            : base(scope, id)
        {
            CreatePublicTargetGroup(vpc, pubAlb, service);
            CreateMonitorTargetGroup(vpc, monAlb, service);
        }

        private void CreatePublicTargetGroup(Vpc vpc,
          ApplicationLoadBalancer alb,
          FargateService service)
        {
            var target = service.LoadBalancerTarget(new LoadBalancerTargetOptions
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
                    Targets = new IApplicationLoadBalancerTarget[] { target }
                });

            alb.Listeners[0].AddTargetGroups(
                "app-listener",
                new AddApplicationTargetGroupsProps
                {
                    TargetGroups = new IApplicationTargetGroup[] { targetGroup }
                });
        }

        private void CreateMonitorTargetGroup(Vpc vpc,
            ApplicationLoadBalancer alb,
            FargateService service)
        {

            var target = service.LoadBalancerTarget(new LoadBalancerTargetOptions
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
                    Targets = new IApplicationLoadBalancerTarget[] { target }
                });

            alb.Listeners[0].AddTargetGroups(
                "monitor-listener",
                new AddApplicationTargetGroupsProps
                {
                    TargetGroups = new IApplicationTargetGroup[] { monitorGroup }
                });
        }
    }
}
