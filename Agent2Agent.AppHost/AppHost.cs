var builder = DistributedApplication.CreateBuilder(args);

var agentA = builder
    .AddProject<Projects.Agent2Agent_AgentA>("AgentA")
    .WithHttpHealthCheck("/health");
    
var agentB = builder
    .AddProject<Projects.Agent2Agent_AgentB>("AgentB")
    .WithHttpHealthCheck("/health");

var agentC = builder
    .AddProject<Projects.Agent2Agent_AgentC>("AgentC")
    .WithHttpHealthCheck("/health");

var agentD = builder
    .AddProject<Projects.Agent2Agent_AgentD>("AgentD")
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.Agent2Agent_Web>("WebApp")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(agentC)
    .WaitFor(agentC)
    .WithReference(agentD)
    .WaitFor(agentD)
    .WithReference(agentB)
    .WaitFor(agentB)
    .WithReference(agentA)
    .WaitFor(agentA)
;

builder.Build().Run();
