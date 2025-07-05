# Agent2Agent Proof-of-Concept

This repository demonstrates a multi-agent proof-of-concept using Microsoft Semantic Kernel and ASP.NET Core Minimal APIs. It features a registration advocacy agent, conversational agent, knowledge graph agent, and internet search agent, all coordinated by an orchestrator and served through a Blazor web frontend.

## Project Structure

- **Agent2Agent.AppHost**  
  The orchestrator that launches and monitors WebFrontend and RegistrationAdvocateAgent.

- **Agent2Agent.Web**  
  Blazor Server web interface (WebFrontend) where users interact with agents.

- **Agent2Agent.AgentA**  
  **RegistrationAdvocateAgent** – Semantic Kernel agent exposing `/api/agent/chat`.

- **Agent2Agent.AgentB**  
  **ChatResponderAgent** – Handles conversational logic at `/chat/respond`.

- **Agent2Agent.AgentC**  
  **KnowledgeGraphAgent** – Serves facts from a knowledge graph at `/kg/query`.

- **Agent2Agent.AgentD**  
  **InternetSearchAgent** – Fetches external data at `/search/query` with Redis caching.

- **Agent2Agent.ServiceDefaults**  
  Shared library for OpenAPI, error handling, caching, and default endpoint mapping.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)  
- Redis instance running (default connection: `localhost:6379`)  
- OpenAI API key configured in each agent’s `appsettings.json`

## Configuration

Each agent’s `appsettings.json` supports the following settings:

```json
{
  "Redis": {
    "ConnectionString": "localhost:6379"
  },
  "OpenAI": {
    "ModelId": "<model-id>",
    "ApiKey": "<your-api-key>"
  },
  "Agents": {
    "ChatResponderAgent": "https://localhost:5001",
    "InternetSearchAgent": "https://localhost:5003"
  }
}
```

Adjust ports and URLs as needed.

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

## Architecture Documentation

See detailed documentation in the `Docs/` folder:

- [architecture.md](Docs/architecture.md)  
- [agents.md](Docs/agents.md)

## Contributing

Contributions are welcome. Please raise issues or pull requests against this proof-of-concept.

## License

This project is released under the MIT License.