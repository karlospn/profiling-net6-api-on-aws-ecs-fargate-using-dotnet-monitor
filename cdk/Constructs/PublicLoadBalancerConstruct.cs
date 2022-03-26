﻿using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ElasticLoadBalancingV2;
using Constructs;

namespace FargateCdkStack.Constructs
{
    public class PublicLoadBalancerConstruct : Construct
    {
        public ApplicationLoadBalancer Alb { get; }
        public ApplicationListener HttpListener { get; }
        public ApplicationListener MonitorListener { get; }
        private SecurityGroup SecurityGroup { get; }

        public PublicLoadBalancerConstruct(Construct scope,
            string id,
            Vpc vpc)
            : base(scope, id)
        {
            SecurityGroup = new SecurityGroup(this,
                "scg-alb-ecs-profiling-dotnet-demo",
                new SecurityGroupProps()
                {
                    Vpc = vpc,
                    AllowAllOutbound = true,
                    Description = "Security group for the public ALB",
                    SecurityGroupName = "scg-alb-ecs-profiling-dotnet-demo"
                });

            SecurityGroup.AddIngressRule(Peer.AnyIpv4(),
                Port.Tcp(80),
                "Allow port 80 ingress traffic");

            SecurityGroup.AddIngressRule(Peer.AnyIpv4(),
                Port.Tcp(52323),
                "Allow port 52323 ingress traffic");

            Alb = new ApplicationLoadBalancer(this,
                "alb-ecs-profiling-dotnet-demo",
                new ApplicationLoadBalancerProps
                {
                    InternetFacing = true,
                    Vpc = vpc,
                    VpcSubnets = new SubnetSelection
                    {
                        OnePerAz = true,
                        SubnetType = SubnetType.PUBLIC,
                    },
                    SecurityGroup = SecurityGroup,
                    LoadBalancerName = "alb-ecs-profiling-dotnet-demo"
                });

            HttpListener = Alb.AddListener("alb-http-listener", new ApplicationListenerProps
            {
                Protocol = ApplicationProtocol.HTTP,
                LoadBalancer = Alb,
                DefaultAction = ListenerAction.FixedResponse(500),
            });

            MonitorListener = Alb.AddListener("alb-monitor-listener", new ApplicationListenerProps
            {
                Port = 52323,
                Protocol = ApplicationProtocol.HTTP,
                LoadBalancer = Alb,
                DefaultAction = ListenerAction.FixedResponse(500),
            });
        }
    }
}