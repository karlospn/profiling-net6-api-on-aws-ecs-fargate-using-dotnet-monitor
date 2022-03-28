using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ElasticLoadBalancingV2;
using Constructs;

namespace FargateCdkStack.Constructs
{
    public class MonitorLoadBalancerConstruct : Construct
    {
        public ApplicationLoadBalancer Alb { get; }

        public MonitorLoadBalancerConstruct(Construct scope,
            string id,
            Vpc vpc)
            : base(scope, id)
        {
            var securityGroup = new SecurityGroup(this,
                "scg-mon-alb-ecs-profiling-dotnet-demo",
                new SecurityGroupProps()
                {
                    Vpc = vpc,
                    AllowAllOutbound = true,
                    Description = "Security group for the monitor ALB",
                    SecurityGroupName = "scg-mon-alb-ecs-profiling-dotnet-demo"
                });

            securityGroup.AddIngressRule(Peer.AnyIpv4(),
                Port.Tcp(52323),
                "Allow port 52323 ingress traffic");

            securityGroup.AddIngressRule(Peer.AnyIpv4(),
                Port.Tcp(9090),
                "Allow port 9090 ingress traffic");

            Alb = new ApplicationLoadBalancer(this,
                "alb-mon-ecs-profiling-dotnet-demo",
                new ApplicationLoadBalancerProps
                {
                    InternetFacing = true,
                    Vpc = vpc,
                    VpcSubnets = new SubnetSelection
                    {
                        OnePerAz = true,
                        SubnetType = SubnetType.PUBLIC,
                    },
                    SecurityGroup = securityGroup,
                    LoadBalancerName = "alb-mon-ecs-prf-dotnet-demo"
                });

            _ = Alb.AddListener("alb-monitor-listener", new ApplicationListenerProps
            {
                Port = 52323,
                Protocol = ApplicationProtocol.HTTP,
                LoadBalancer = Alb,
                DefaultAction = ListenerAction.FixedResponse(500),
            });

            _ = Alb.AddListener("alb-prom-listener", new ApplicationListenerProps
            {
                Port = 9090,
                Protocol = ApplicationProtocol.HTTP,
                LoadBalancer = Alb,
                DefaultAction = ListenerAction.FixedResponse(500),
            });

            _ = Alb.AddListener("alb-grafana-listener", new ApplicationListenerProps
            {
                Port = 3000,
                Protocol = ApplicationProtocol.HTTP,
                LoadBalancer = Alb,
                DefaultAction = ListenerAction.FixedResponse(500),
            });
        }
    }
}
