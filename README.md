# Agent2Agent Proof-of-Concept

This repository demonstrates a multi-agent proof-of-concept using Microsoft Semantic Kernel and ASP.NET Core Minimal APIs. It features a registration advocacy agent, conversational agent, knowledge graph agent, and internet search agent, all coordinated by an orchestrator and served through a Blazor web frontend.

## Project Structure

- **Agent2Agent.AppHost**  
  The orchestrator that launches and monitors all agents and the WebFrontend, coordinating their lifecycle and health.

- **Agent2Agent.Web**  
  Blazor Server web interface (WebFrontend) where users interact with agents.

- **Agent2Agent.AgentA**
  **RegistrationAdvocateAgent** – Semantic Kernel agent exposing `/api/agent/chat`.
  - Hosts a `ChatCompletionAgent` named "RegistrationAdvocate".
  - Registers plugins for inter-agent calls to ChatResponder and InternetSearch.

- **Agent2Agent.AgentB**
  **ChatResponderAgent** – A2A server for conversational logic.
  - Exposes endpoints for chat response.
  - Uses A2AClient to query KnowledgeGraphAgent and InternetSearchAgent.

- **Agent2Agent.AgentC**
  **KnowledgeGraphAgent** – A2A server for knowledge graph queries.
  - Handles `/kg/query` via Semantic Kernel.
  - Uses Redis for vector storage and embeddings.

- **Agent2Agent.AgentD**
  **InternetSearchAgent** – A2A server for internet search.
  - Handles `/search/query` using Semantic Kernel and plugins.
  - Planned: Redis caching and external search API integration.

- **Agent2Agent.ServiceDefaults**  
  Shared library for OpenAPI, error handling, caching, and default endpoint mapping.
  All agents and services reference this for consistent middleware and integrations.

- **DatasetCreator**
  Imports CSV and PDF datasets into Redis for the KnowledgeGraphAgent containing sample data about
  vehicle registration information.

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- Redis instance running (default connection: `localhost:6379`)
- OpenAI API key configured in each agent’s `appsettings.json`


## Getting Started

1. Clone this repository.
2. Restore and build all projects:
   ```bash
   dotnet restore
   dotnet build
   ```
3. Start Redis (if not running).
4. Launch the orchestrator (runs all services and agents):
   ```bash
   dotnet run --project Agent2Agent.AppHost
   ```
5. Open the web frontend in your browser:
   `https://localhost:5000`

## Documentation

- [`Docs/architecture.md`](Docs/architecture.md): System architecture, diagrams, and startup sequence.
- [`Docs/agents.md`](Docs/agents.md): Agent roles, endpoints, and sequence diagrams.

## Contributing

Contributions are welcome. Please raise issues or pull requests against this proof-of-concept.

## License

This project is released under the MIT License.