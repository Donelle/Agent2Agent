var builder = DistributedApplication.CreateBuilder(args);

var agentB = builder
		.AddProject<Projects.Agent2Agent_AgentB>("AgentB")
		.WithHttpHealthCheck("/health");

var agentA = builder
    .AddProject<Projects.Agent2Agent_AgentA>("AgentA")
    .WithHttpHealthCheck("/health")
    .WaitFor(agentB);

var agentC = builder
    .AddProject<Projects.Agent2Agent_AgentC>("AgentC")
    .WithHttpHealthCheck("/health")
    .WaitFor(agentB);

var agentD = builder
    .AddProject<Projects.Agent2Agent_AgentD>("AgentD")
    .WithHttpHealthCheck("/health")
		.WaitFor(agentB);

builder.AddProject<Projects.Agent2Agent_Web>("WebApp")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WaitFor(agentC)
    .WaitFor(agentD)
    .WaitFor(agentA);

builder.Build().Run();
