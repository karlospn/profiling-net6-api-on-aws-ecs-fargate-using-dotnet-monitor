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
            FargateService appService,
            FargateService promService,
            FargateService grafService) 
            : base(scope, id)
        {
            CreatePublicTargetGroup(vpc, pubAlb, appService);
            CreateMonitorTargetGroup(vpc, monAlb, appService);
            CreatePrometheusTargetGroup(vpc, monAlb, promService);
            CreateGrafanaTargetGroup(vpc, monAlb, grafService);
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

        private void CreatePrometheusTargetGroup(Vpc vpc,
            ApplicationLoadBalancer alb,
            FargateService service)
        {

            var target = service.LoadBalancerTarget(new LoadBalancerTargetOptions
            {
                ContainerPort = 9090,
                Protocol = Amazon.CDK.AWS.ECS.Protocol.TCP,
                ContainerName = "prometheus-app"
            });

            var monitorGroup = new ApplicationTargetGroup(this,
                "tg-monitor-prometheus-demo",
                new ApplicationTargetGroupProps
                {
                    TargetGroupName = "tg-prometheus",
                    Vpc = vpc,
                    TargetType = TargetType.IP,
                    ProtocolVersion = ApplicationProtocolVersion.HTTP1,
                    Protocol = ApplicationProtocol.HTTP,
                    HealthCheck = new HealthCheck
                    {
                        Protocol = Amazon.CDK.AWS.ElasticLoadBalancingV2.Protocol.HTTP,
                        HealthyThresholdCount = 2,
                        Path = "/healthy",
                        Port = "9090",
                        Interval = Duration.Millis(100000),
                        Timeout = Duration.Millis(60000),
                        UnhealthyThresholdCount = 10,
                        HealthyHttpCodes = "200,404"
                    },
                    Port = 9090,
                    Targets = new IApplicationLoadBalancerTarget[] { target }
                });

            alb.Listeners[1].AddTargetGroups(
                "prometheus-listener",
                new AddApplicationTargetGroupsProps
                {
                    TargetGroups = new IApplicationTargetGroup[] { monitorGroup }
                });
        }

        private void CreateGrafanaTargetGroup(Vpc vpc,
            ApplicationLoadBalancer alb,
            FargateService service)
        {

            var target = service.LoadBalancerTarget(new LoadBalancerTargetOptions
            {
                ContainerPort = 3000,
                Protocol = Amazon.CDK.AWS.ECS.Protocol.TCP,
                ContainerName = "grafana-app"
            });

            var monitorGroup = new ApplicationTargetGroup(this,
                "tg-monitor-grafana",
                new ApplicationTargetGroupProps
                {
                    TargetGroupName = "tg-grafana",
                    Vpc = vpc,
                    TargetType = TargetType.IP,
                    ProtocolVersion = ApplicationProtocolVersion.HTTP1,
                    Protocol = ApplicationProtocol.HTTP,
                    HealthCheck = new HealthCheck
                    {
                        Protocol = Amazon.CDK.AWS.ElasticLoadBalancingV2.Protocol.HTTP,
                        HealthyThresholdCount = 3,
                        Path = "/api/health",
                        Port = "3000",
                        Interval = Duration.Millis(10000),
                        Timeout = Duration.Millis(8000),
                        UnhealthyThresholdCount = 10,
                        HealthyHttpCodes = "200"
                    },
                    Port = 3000,
                    Targets = new IApplicationLoadBalancerTarget[] { target }
                });

            alb.Listeners[2].AddTargetGroups(
                "grafana-listener",
                new AddApplicationTargetGroupsProps
                {
                    TargetGroups = new IApplicationTargetGroup[] { monitorGroup }
                });
        }
    }
}
