# Agent2Agent Architecture Overview

This document provides a high-level overview of the Agent2Agent proof-of-concept, outlining its structure, component responsibilities, and runtime relationships.

---

## Project Structure

- **Agent2Agent.Web (WebFrontend)**  
  The Blazor Server web interface where users interact with agents.  
  - Uses Razor Components and Output Cache.  
  - Communicates with orchestrator and agents via HTTP.

- **Agent2Agent.AppHost (Orchestrator)**
  The orchestrator launches and monitors the WebFrontend and RegistrationAdvocateAgent, as well as other agents.
  - Runs and monitors all agents and services, performing health checks.
  - Configures and serves as the entry point for the distributed application.

- **Agent2Agent.AgentA (RegistrationAdvocateAgent)**  
  A Semantic Kernel–driven agent exposing `/api/agent/chat`.  
  - Hosts a `ChatCompletionAgent` named "RegistrationAdvocate".  
  - Registers `RegistryAgentPlugin` and `InternetSearchAgentPlugin`.
  - Invokes AgentB (RegistryAgent) and AgentD (InternetSearchAgent) via A2AClient to enrich responses on vehicle registration topics.
  - Restricts replies to vehicle registration and related queries.

- **Agent2Agent.AgentB (RegistryAgent)**
  The A2A server hosting agent registration, discovery, and inter-agent communication.
  - Configured via `builder.Services.AddA2AServer` with AgentCard settings and `builder.Services.AddA2AClient` for KnowledgeGraphAgent.
  - Registers `RegistryAgentLogic` as `IAgentLogicInvoker` and `KnowledgeGraphAgentPlugin` in DI.
  - Hosts a Semantic Kernel `ChatCompletionAgent` named **VehicleRegistrationAssistant**.
  - Maps A2A endpoints using `app.MapA2AWellKnown()` and `app.MapA2AEndpoint()`.
  - Uses A2A client to invoke KnowledgeGraphAgent for factual enrichment.

- **Agent2Agent.AgentC (KnowledgeGraphAgent)**
  The A2A server hosting knowledge graph functionality.
  - Configured via `builder.AddServiceDefaults()` and `builder.Services.AddDependencies()`.
  - Registers `KnowledgeGraphAgentLogic` as `IAgentLogicInvoker`, `FactStorePlugin`, and embedding/vector store providers.
  - Hosts a `ChatCompletionAgent` named **KnowledgeGraphAgent** with `search_knowledgebase` function.
  - Maps A2A endpoints using `app.MapA2AWellKnown()` and `app.MapA2AEndpoint()`.
  - Persists embeddings in Redis and ensures vector store index on startup.

- **Agent2Agent.AgentD (InternetSearchAgent)**
  The A2A server hosting internet search functionality.
  - Configured via `builder.Services.AddAgentDependencies()` and `AddA2AServer` with AgentCard settings.
  - Registers `InternetSearchAgentLogic` as `IAgentLogicInvoker` and `SearchPlugin` for kernel functions.
  - Hosts a `ChatCompletionAgent` named **InternetSearchAgent** with `search_internet` function.
  - Maps A2A endpoints using `app.MapA2AWellKnown()` and `app.MapA2AEndpoint()`.
  - Planned enhancements:
    - Integrate Redis caching for search results.
    - Connect to external search APIs for live data.

- **Agent2Agent.ServiceDefaults**
  Shared library for common bootstrapping routines.
  - Provides extensions for OpenAPI, exception handling, caching, and default endpoint mapping.

- **DatasetCreator**
  Data ingestion tool for KnowledgeGraphAgent.
  - Processes CSV and PDF files containing vehicle registration data.
  - Generates OpenAI embeddings for semantic search.
  - Populates Redis vector store with knowledge chunks, metadata, and embeddings.
  - Supports batch updates and schema compatibility with AgentC.

---

## Component Diagram

```mermaid
flowchart TD
  subgraph Orchestrator
    AppHost[AppHost<br/>- Launches/monitors all agents<br/>- Health checks]
  end
  subgraph Web
    WebFrontend[WebFrontend<br/>- Blazor Server<br/>- User chat UI<br/>- Calls /api/agent/chat]
  end
  subgraph Agents
    AgentA[RegistrationAdvocateAgent<br/>- POST /api/agent/chat<br/>- Plugins: ChatResponder, InternetSearch]
    AgentB[RegistryAgent<br/>- POST /tasks<br/>- GET /.well-known/agent.json]
    AgentC[KnowledgeGraphAgent<br/>- POST /tasks<br/>- GET /.well-known/agent.json]
    AgentD[InternetSearchAgent<br/>- POST /tasks<br/>- GET /.well-known/agent.json]
  end
  DatasetCreator[DatasetCreator<br/>- Populates Redis<br/>- CSV/PDF ingestion]
  Redis[(Redis Vector Store<br/>- Embeddings<br/>- Knowledge Chunks)]
  ExtAPI[(ExternalSearchAPI)]

  AppHost -- launches & health-checks --> WebFrontend
  AppHost -- launches & health-checks --> AgentA
  AppHost -- launches & health-checks --> AgentB
  AppHost -- launches & health-checks --> AgentC
  AppHost -- launches & health-checks --> AgentD

  WebFrontend -- HTTP: /api/agent/chat --> AgentA

  AgentA -- A2AClient: /tasks --> AgentB
  AgentA -- A2AClient: /tasks --> AgentD

  AgentB -- A2AClient: /tasks --> AgentC
  AgentB -- A2AClient: /tasks --> AgentD

  AgentC -- Redis Vector Search --> Redis
  AgentD -- Redis Cache --> Redis
  AgentD -- External Search API --> ExtAPI

  DatasetCreator -- Populates --> Redis

  AgentA -- references --> ServiceDefaults
  AgentB -- references --> ServiceDefaults
  AgentC -- references --> ServiceDefaults
  AgentD -- references --> ServiceDefaults
  WebFrontend -- references --> ServiceDefaults
```

---

## Sequence Diagram

```mermaid
sequenceDiagram
  participant User
  participant Web as WebFrontend
  participant A as RegistrationAdvocateAgent
  participant B as RegistryAgent
  participant C as KnowledgeGraphAgent
  participant D as InternetSearchAgent
  participant Redis as Redis
  participant DatasetCreator
  participant ExtAPI as ExternalSearchAPI

  DatasetCreator->>Redis: Ingest CSV/PDF, generate embeddings, store knowledge chunks
  User->>Web: Enters query
  Web->>A: POST /api/agent/chat { text }
  A->>B: POST /tasks { text }
  B->>C: POST /tasks { concepts }
  alt KG has data
    C-->>B: 200 { facts }
  else KG missing data
    C-->>A: 204 No Content
    A->>D: POST /tasks { text }
    alt Cache hit
      D-->>A: 200 { cachedResults }
    else Cache miss
      D->>ExtAPI: GET /?q={text}
      ExtAPI-->>D: 200 { searchResults }
      D-->Redis: SET key, value, TTL
      D-->>A: 200 { searchResults }
    end
  end
  A-->>Web: 200 { combined reply }
```

---

## Startup Sequence
- **AppHost** ([AppHost.cs](Agent2Agent.AppHost/AppHost.cs:1))
  1. Creates the distributed application builder.
  2. Adds WebFrontend and RegistrationAdvocateAgent with health checks.
  3. Builds and runs all components.

- **RegistrationAdvocateAgent** ([Program.cs](Agent2Agent.AgentA/Program.cs:1))  
  1. Configures OpenAI chat client and Semantic Kernel plugins.  
  2. Registers ChatResponderAgentPlugin and InternetSearchAgentPlugin.  
  3. Maps OpenAPI and default endpoints.  
  4. Runs the agent host.

---

## Shared Library Patternse and intgrations

All services and agents reference `ServiceDefaults` to apply consistent middleware and integrations:

- **OpenAPI**: Automated API endpoint generation.  
- **Exception Handling**: ProblemDetails and global error handlers.  
- **Caching**: Redis caching for AgentD; output caching for WebFrontend.  
- **Default Endpoints**: Uniform endpoint mapping across services.

- [Microsoft Semantic Kernel Agents](https://learn.microsoft.com/en-us/semantic-kernel/frameworks/agent/?pivots=programming-language-csharp)  
- [A2A Project Documentation](https://a2aproject.github.io/A2A/v0.2.5/)  