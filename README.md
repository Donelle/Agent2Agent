# Agent2Agent — Proof of Concept

This repository contains a proof-of-concept multi-agent system built with Microsoft Semantic Kernel and ASP.NET Core. It demonstrates agent-to-agent (A2A) registration, discovery, inter-agent task delegation, and a Blazor web frontend for user interaction.

## Quick architecture summary

- Orchestrator (AppHost) launches and health-checks agents and the Web frontend.
- Agent A (Customer Advocate) — user-facing semantic agent (POST /api/agent/chat).
- Agent B (Registry) — manages agent registration/discovery; A2A endpoints (`/tasks`, `/.well-known/agent.json`).
- Agent C (Knowledge Graph) — vector search and factual grounding (Redis + embeddings).
- Agent D (Internet Search) — external search and caching.
- DatasetCreator — ingests CSV/PDF, creates embeddings, populates Redis for Agent C.

## Project structure (high level)

- Agent2Agent.AppHost — orchestrator and entry point.
- Agent2Agent.Web — Blazor Server web UI (chat frontend).
- Agent2Agent.AgentA — Customer Advocate agent (POST /api/agent/chat).
- Agent2Agent.AgentB — Registry agent (A2A server).
- Agent2Agent.AgentC — Knowledge Graph agent (vector store + embeddings).
- Agent2Agent.AgentD — Internet Search agent (external search + caching).
- Agent2Agent.ServiceDefaults — shared bootstrap/middleware/extensions.
- DatasetCreator — data ingestion and embedding pipeline.
- Docs/ — architecture and agent role documentation.

## Prerequisites

- .NET 9 SDK
- Docker (to run Redis via docker-compose)
- OpenAI API key (or other embedding/chat provider) configured in each agent's appsettings

## Getting started (local development)

1. Clone repository
2. Restore and build
```bash
dotnet restore
dotnet build
```

3. Start Redis via Docker Compose (uses `docker_compose.yaml`)
```bash
docker-compose -f docker_compose.yaml up -d
```

4. (Optional) Ingest sample data into Redis
```bash
dotnet run --project DatasetCreator
```
See [`DatasetCreator/README.md`](DatasetCreator/README.md:1) for options and sample data formats.

5. Launch the orchestrator (starts AppHost, Web, and configured agents)
```bash
dotnet run --project Agent2Agent.AppHost
```

6. Open the Web frontend in your browser (default)
https://localhost:5000
If custom URLs are configured in `launchSettings.json`, use those instead.


## How the system works (short)

- The Web frontend sends user queries to AgentA.
- AgentA uses Semantic Kernel chat completion to craft responses and may invoke other agents for factual enrichment.
- AgentB (Registry) provides discovery and delegates tasks to AgentC (KnowledgeGraph) and AgentD (InternetSearch) as needed.
- DatasetCreator populates Redis so AgentC can answer fact-based queries through semantic vector search.

## Documentation and further reading

- [`Docs/architecture.md`](Docs/architecture.md:1) — architecture overview, diagrams, startup sequence.
- [`Docs/agents.md`](Docs/agents.md:1) — agent roles, endpoints, and sequences.
- Agent-specific READMEs: [`Agent2Agent.AgentA/README.md`](Agent2Agent.AgentA/README.md:1), [`Agent2Agent.AgentB/README.md`](Agent2Agent.AgentB/README.md:1), etc.
- A2A Spec: https://a2aproject.github.io/A2A/v0.2.5/
- Microsoft Semantic Kernel: https://learn.microsoft.com/en-us/semantic-kernel/

## Troubleshooting (common)

- OpenAI errors: verify API key, model id, and network access.
- Redis: ensure docker container is running and connection string matches.
- Inter-agent calls: ensure agents are running and reachable; check logs for plugin/DI registration errors.

## Contributing

Contributions welcome. Open issues or PRs and include tests or usage notes where relevant.

## License

MIT

---