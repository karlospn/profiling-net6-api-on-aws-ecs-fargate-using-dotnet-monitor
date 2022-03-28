# Profiling a .NET6 app running on AWS ECS Fargate with dotnet-monitor

This repository contains a practical example about how to deploy an application with ``dotnet-monitor`` as a sidecar container on AWS ECS Fargate.

The repository contains a .NET6 API and a CDK app that creates the AWS Resources needed to run the demo.

# Dependencies

- _Docker installed on your machine._   
The API is containerized and pushed into ECR with the CDK app. When the ``cdk deploy`` command is executed the app container is created, so you'll need docker installed on your machine.

# AWS Resources

![components](https://raw.githubusercontent.com/karlospn/profiling-net6-api-on-aws-ecs-fargate-demo/main/docs/after.png)

The CDK app will create the following resources:

- VPC.
- 2 Public Subnets.
- A public Application Load Balancer that listens on port 80 (I didn't want to set an SSL certificate...).  
  - This ALB is used to access the NET6 Web API. 
- A public Application Load Balancer that listens on port 52323
  - This one is used to interact with ``dotnet-monitor`` (you could use a single ALB and set 2 listeners with 2 target groups, but in a realistic scenario you really don't want to open a port on your internet facing ALB, instead you'll create a secondary internal ALB and use it to interact with the ``dotnet-monitor`` API).
- An ECS Task Definition with 2 containers: API container + dotnet-monitor container.




