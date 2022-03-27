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

            var publicAlb = new PublicLoadBalancerConstruct(this,
                "aws-fargate-profiling-dotnet-demo-public-alb-construct",
                vpc.Vpc);

            var monitorAlb = new MonitorLoadBalancerConstruct(this,
                "aws-fargate-profiling-dotnet-demo-monitor-alb-construct",
                vpc.Vpc);

            var service = new EcsServiceConstruct(this,
                "aws-fargate-profiling-dotnet-demo-ecs-service-construct",
                vpc.Vpc,
                fg.Cluster,
                publicAlb.Alb,
                monitorAlb.Alb);

            _ = new TargetGroupConstruct(this,
                "aws-fargate-profiling-dotnet-demo-target-groups",
                vpc.Vpc,
                publicAlb.Alb,
                monitorAlb.Alb,
                service.FargateService);
        }
    }
}
