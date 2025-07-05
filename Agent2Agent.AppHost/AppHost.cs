var builder = DistributedApplication.CreateBuilder(args);

var agentA = builder
    .AddProject<Projects.Agent2Agent_AgentA>("agentA")
    .WithHttpHealthCheck("/health");

var agentB = builder
    .AddProject<Projects.Agent2Agent_AgentB>("agentB")
    .WithHttpHealthCheck("/health");

var agentC = builder
    .AddProject<Projects.Agent2Agent_AgentC>("agentC")
    .WithHttpHealthCheck("/health");

var agentD = builder
    .AddProject<Projects.Agent2Agent_AgentD>("agentD")
    .WithHttpHealthCheck("/health");
   
builder.AddProject<Projects.Agent2Agent_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(agentA)
    .WaitFor(agentA)
    .WithReference(agentB)
    .WaitFor(agentB)
    .WithReference(agentC)
    .WaitFor(agentC)
    .WithReference(agentD)
    .WaitFor(agentD);

builder.Build().Run();
