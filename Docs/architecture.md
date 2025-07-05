# Agent2Agent Architecture Overview

This document provides a high-level overview of the Agent2Agent proof-of-concept, outlining its structure, component responsibilities, and runtime relationships.

---

## Project Structure

- **Agent2Agent.Web (WebFrontend)**  
  The Blazor Server web interface where users interact with agents.  
  - Uses Razor Components and Output Cache.  
  - Communicates with orchestrator and agents via HTTP.

- **Agent2Agent.AppHost (Orchestrator)**  
  Manages running instances of the WebFrontend and RegistrationAdvocateAgent.  
  - Launches and monitors WebFrontend and RegistrationAdvocateAgent.  
  - Configures health checks and service references.

- **Agent2Agent.AgentA (RegistrationAdvocateAgent)**  
  A Semantic Kernel–driven agent exposing `/api/agent/chat`.  
  - Hosts a `ChatCompletionAgent` named "RegistrationAdvocate".  
  - Registers `ChatResponderAgentPlugin` and `InternetSearchAgentPlugin`.  
  - Invokes AgentB and AgentD via A2AClient to enrich responses on vehicle registration topics.  
  - Restricts replies to vehicle registration and related queries.

- **Agent2Agent.AgentB (ChatResponderAgent)**
  The A2A server hosting vehicle registration assistant logic.
  - Configured via `builder.Services.AddA2AServer` with AgentCard settings and `builder.Services.AddA2AClient` for KnowledgeGraphAgent.
  - Registers `ChatResponderAgentLogic` as `IAgentLogicInvoker` and `KnowledgeGraphAgentPlugin` in dependency injection.
  - Hosts a Semantic Kernel `ChatCompletionAgent` named **VehicleRegistrationAssistant**.
  - Maps A2A endpoints using `app.MapA2AWellKnown()` and `app.MapA2AEndpoint()`.
  - Uses A2A client to invoke KnowledgeGraphAgent for factual enrichment.

- **Agent2Agent.AgentC (KnowledgeGraphAgent)**  
  The facts and relationships store.  
  - Exposes `POST /kg/query`.  
  - Maintains a knowledge graph of domain facts.  
  - Returns 200 OK with facts or 204 No Content if none found.

- **Agent2Agent.AgentD (InternetSearchAgent)**  
  The external data fetcher.  
  - Exposes `POST /search/query`.  
  - Uses Redis (`StackExchange.Redis`) for caching.  
  - On cache miss, calls an external search API and caches results.  
  - Returns search results for fallback enrichment.

- **Agent2Agent.ServiceDefaults**  
  Shared library for common bootstrapping routines.  
  - Provides extensions for OpenAPI, exception handling, caching, and default endpoint mapping.

---

## Component Diagram

```mermaid
flowchart LR
  subgraph Orchestrator
    A[AppHost]
  end
  subgraph Web
    Web[WebFrontend]
  end
  subgraph Agents
    Reg[RegistrationAdvocateAgent]
    Chat[ChatResponderAgent]
    KG[KnowledgeGraphAgent]
    Search[InternetSearchAgent]
  end
  subgraph Shared
    SD[ServiceDefaults]
  end

  A -->|launch & health-check| Web
  A -->|HTTP: /api/agent/chat| Reg
  Reg -->|respond_to_chat| Chat
  Chat -->|POST /kg/query| KG
  KG -- 204 --> Chat
  Chat -->|POST /search/query| Search
  Search -->|cache/API| Chat

  Web -->|references| SD
  Reg -->|references| SD
  Chat -->|references| SD
  KG -->|references| SD
  Search -->|references| SD
```

---

## Startup Sequence

- **AppHost** ([AppHost.cs](Agent2Agent.AppHost/AppHost.cs:1))  
  1. Creates distributed application builder.  
  2. Adds WebFrontend and RegistrationAdvocateAgent with health checks.  
  3. Builds and runs all components.

- **RegistrationAdvocateAgent** ([Program.cs](Agent2Agent.AgentA/Program.cs:1))  
  1. Configures OpenAI chat client and Semantic Kernel plugins.  
  2. Registers ChatResponderAgentPlugin and InternetSearchAgentPlugin.  
  3. Maps OpenAPI and default endpoints.  
  4. Runs the agent host.

---

## Shared Library Patterns

All services and agents reference `ServiceDefaults` to apply consistent middleware:

- **OpenAPI**: Automated API endpoint generation.  
- **Exception Handling**: ProblemDetails and global error handlers.  
- **Caching**: Redis caching for AgentD; output caching for WebFrontend.  
- **Default Endpoints**: Uniform endpoint mapping.

---

## References

- [Microsoft Semantic Kernel Agents](https://learn.microsoft.com/en-us/semantic-kernel/frameworks/agent/?pivots=programming-language-csharp)  
- [A2A Project Documentation](https://a2aproject.github.io/A2A/v0.2.5/)  