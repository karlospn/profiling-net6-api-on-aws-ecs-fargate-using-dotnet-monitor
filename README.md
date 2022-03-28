# Profiling a .NET6 app running on AWS ECS Fargate with dotnet-monitor

This repository contains a practical example about how to deploy an application with ``dotnet-monitor`` as a sidecar container on AWS ECS Fargate.

The repository contains an app and a CDK app that creates the AWS Resources needed to run the demo.

# Requirements

- Docker  
The app is containerized and deployed into ECR within the CDK app. When you executen the ``cdk deploy`` command the app container is going to be created, so you'll need docker installed on your machine.

# AWS Resources

![components](https://raw.githubusercontent.com/karlospn/profiling-net6-api-on-aws-ecs-fargate-demo/main/docs/after.png)

The CDK app will create the following resources:

- VPC.
- 2 Public Subnets.
- A public Application Load Balancer that listens on port 80 (I didn't want to set an SSL certificate...).  
  - This ALB is used to access the app-container API. 
- A public Application Load Balancer that listens on port 52323
  - This one is used to access the dotnet-monitor (you could use a single ALB and set 2 listeners with 2 target groups, but in a more realistic scenario you don't want to open a port in a public ALB, instead you'll create a secondary internal ALB and use it to access the dotnet-monitor API).
- An ECS Task Definition with 2 containers: app container + dotnet-monitor container




