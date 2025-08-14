# Agent2Agent Proof-of-Concept

This repository demonstrates a multi-agent proof-of-concept using Microsoft Semantic Kernel and ASP.NET Core Minimal APIs. It features a registration advocacy agent, conversational agent, knowledge graph agent, and internet search agent, all coordinated by an orchestrator and served through a Blazor web frontend.

## Project Structure

- **Agent2Agent.AppHost**  
  The orchestrator that launches and monitors all agents and the WebFrontend, coordinating their lifecycle and health.

- **Agent2Agent.Web**  
  Blazor Server web interface (WebFrontend) where users interact with agents.

- **Agent2Agent.AgentA**
  **CustomerAdvocateAgent** – Semantic Kernel agent exposing `/api/agent/chat`.
  - Hosts a `ChatCompletionAgent` named "RegistrationAdvocate".
  - Registers plugins for inter-agent calls to ChatResponder and InternetSearch.

- **Agent2Agent.AgentB**
  **RegistryAgent** – A2A server for agent registration, discovery, and inter-agent communication.
  - Exposes `/tasks` and `/.well-known/agent.json` endpoints for A2A protocol.
  - Registers and manages agent metadata, and delegates queries to KnowledgeGraphAgent and InternetSearchAgent as needed.

- **Agent2Agent.AgentC**
  **KnowledgeGraphAgent** – A2A server for knowledge graph queries.
  - Exposes `/tasks` and `/.well-known/agent.json` endpoints for A2A protocol.
  - Uses Redis for vector storage and embeddings.

- **Agent2Agent.AgentD**
  **InternetSearchAgent** – A2A server for internet search.
  - Exposes `/tasks` and `/.well-known/agent.json` endpoints for A2A protocol.
  - Planned: Redis caching and external search API integration.

- **Agent2Agent.ServiceDefaults**  
  Shared library for OpenAPI, error handling, caching, and default endpoint mapping.
  All agents and services reference this for consistent middleware and integrations.

- **DatasetCreator**
  Imports CSV and PDF datasets into Redis for the KnowledgeGraphAgent containing sample data about
  vehicle registration information.

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- Docker (for running Redis via Docker Compose)
- OpenAI API key configured in each agent’s `appsettings.json`

## Getting Started

1. Clone this repository.
2. Restore and build all projects:
   ```bash
   dotnet restore
   dotnet build
   ```
3. Start Redis using Docker Compose:
   ```bash
   docker-compose -f docker_compose.yaml up -d
   ```
   This will launch a Redis server using the provided `docker_compose.yaml` file.
4. Load vehicle data into Redis using DatasetCreator:
   ```bash
   dotnet run --project DatasetCreator
   ```
   See [`DatasetCreator/README.md`](DatasetCreator/README.md) for details and advanced options.
5. Launch the orchestrator (runs all services and agents):
   ```bash
   dotnet run --project Agent2Agent.AppHost
   ```
6. Open the web frontend in your browser:
   https://localhost:5000

## Documentation

- [`Docs/architecture.md`](Docs/architecture.md): System architecture, diagrams, and startup sequence.
- [`Docs/agents.md`](Docs/agents.md): Agent roles, endpoints, and sequence diagrams.

## Contributing

Contributions are welcome. Please raise issues or pull requests against this proof-of-concept.

## License

This project is released under the MIT License.