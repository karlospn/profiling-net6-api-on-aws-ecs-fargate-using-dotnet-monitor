using Amazon.CDK;
using Constructs;
using FargateCdkStack.Constructs;

namespace FargateCdkStack.Stacks
{
    public class CdkStack : Stack
    {
        internal CdkStack(Construct scope,
            string id,
            IStackProps props = null)
            : base(scope, id, props)
        {
            var vpc = new VpcConstruct(this,
                "aws-fargate-profiling-dotnet-demo-vpc-construct");

            var fg = new FargateClusterConstruct(this,
                "aws-fargate-profiling-dotnet-demo-cluster-construct", 
                vpc.Vpc);

            var alb = new PublicLoadBalancerConstruct(this,
                "aws-fargate-profiling-dotnet-demo-public-alb-construct",
                vpc.Vpc);

            _ = new EcsServiceConstruct(this,
                "aws-fargate-profiling-dotnet-demo-ecs-service-construct",
                vpc.Vpc,
                fg.Cluster,
                alb.Alb,
                alb.HttpListener,
                alb.MonitorListener);
        }
    }
}
