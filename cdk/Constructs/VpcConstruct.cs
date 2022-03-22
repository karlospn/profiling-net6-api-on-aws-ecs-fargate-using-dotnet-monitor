using Amazon.CDK.AWS.EC2;
using Constructs;

namespace FargateCdkStack.Constructs
{
    public class VpcConstruct : Construct
    {
        public Vpc Vpc { get; set; }
        public VpcConstruct(Construct scope, string id)
            : base(scope, id)
        {
            Vpc = new Vpc(this,
                "vpc-ecs-profiling-dotnet-demo",
                new VpcProps
                {
                    Cidr = "10.30.0.0/16",
                    MaxAzs = 2,
                    NatGateways = 0,
                    VpcName = "vpc-ecs-profiling-dotnet-demo",
                    SubnetConfiguration = new ISubnetConfiguration[]
                    {
                        new SubnetConfiguration
                        {
                            Name = "subnet-public-ecs-profiling-dotnet-demo",
                            CidrMask = 24,
                            SubnetType = SubnetType.PUBLIC,
                        }
                    }
                });
        }
    }
}
