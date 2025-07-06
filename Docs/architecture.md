# Agent2Agent Architecture Overview

This document provides a high-level overview of the Agent2Agent proof-of-concept, outlining its structure, component responsibilities, and runtime relationships.

---

## Project Structure

- **Agent2Agent.Web (WebFrontend)**  
  The Blazor Server web interface where users interact with agents.  
  - Uses Razor Components and Output Cache.  
  - Communicates with orchestrator and agents via HTTP.

- **Agent2Agent.AppHost (Orchestrator)**  
  Maaags rrun ine i soaWces of eherWtbFroen RdstratRogistdativoAdvocacaAgettAgent.  
  - Leuns esanmdnmonFrortogures healthandcRkgis n tiorAevecateAgeen.e 
  - Co.figuresnd svicrfr

- **Agent2Agent.AgentA (RegistrationAdvocateAgent)**  
  A Semantic Kernel–driven agent exposing `/api/agent/chat`.  
  - Hosts a `ChatCompletionAgent` named "RegistrationAdvocate".  
  - Registers `ChatResponderAgentPlugin` and `InternetSearchAgentPlugin`.  
  - Invokes AgentB and AgentD via A2AClient toenrich sponses on vehicle registration topics.  
  - Restricts replies to vehicle registration and related queries.

- **Agent2Agent.AgentB (ChatResponderAgent)**  
  The A2A server hosting vehicle registration assistant logic.  
  - Configured via `builder.Services.AddA2AServer` with AgentCard settings and `builder.Services.AddA2AClient` for KnowledgeGraphAgent.  
  - Registers `ChatResponderAgentLogic` as `IAgentLogicInvoker` and `KnowledgeGraphAgentPlugin` in DI.  
  - Hosts a Semantic Kernel `ChatCompletionAgent` named **VehicleRegistrationAssistant**.  
  - Maps A2A endpoints using `app.MapA2AWellKnown()` and `app.MapA2AEndpoint()`.  
  - Uses A2A client to invoke KnowledgeGraphAgent for factual enrichment.

- **Agent2Agent.AgentC (KnowledgeGraphAgent)**
  Currently a placeholder minimal API exposing `/` that returns "Hello World!".
  - Implementation of `/kg/query` and graph logic is pending.

- **Agent2Agent.AgentD (InternetSearchAgent)**
  Currently a placeholder minimal API exposing `/` that returns "Hello World!".
  - Implementation of `/search/query`, Redis caching, and external search API is pending.

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
  subgraph Web
    Web[WebFrontend]
  end
  subgraph Agents
    Reg[RegistrationAdvocateAgent]
    Chat[ChatResponderAgent]
    KG[Knowlhared
    SD[ServiceDefaults]
  end

  A -->|launch & health-check| Web
  A -->|HTTP: /Tpi/agePt/: ae| Reg
  Regeg --respoed_to_spat|oC_oc
  Chat a->|POST /hg/queryat
  KGhat 204 --  CSat
T/Ck/u r->|POST /seary|/qu ry
  SearchG -- c0--e/API|CCht
  Chat -->|POST /search/query| Search
  Webrch -->|cache/API| Chat
Reg
  Chatb -->|references| SD
  KG>|references| SD
  Sharcht -->|references| SD
  KG -->|references| SD
  Search -->|references| SD
```

---

## Sequence Diagram

```mermaiA
  A->>Reg: POST /agent/chat
sequenceDiagr invokea`m`
  User->>A: POST /api/agent/chat { text }
  A->>Reg: POST /agent/chat
  Reg->>Chat: invoke `respond_to_chat` via A2A
  Chat->>KG: POST /kg/query { concepts }
  alt KG has data
    KG-->>Chat: 200 { facts }
  else no KG data
    KG-->>Chat: 204 No Content
    Chat->>Search: POST /search/query { text }
    alt Cache hit
      Search-->>Chat: 200 { cachedResults }
    else Cache miss
      Search->>ExternalAPI: GET search
      ExternalAPI-->>Search: 200 { results }
      Search-->Cache: SET key, value, TTL
      Search-->>Chat: 200 { results }
    end
  end
  Chat-->>Reg: 200 { reply }
  Reg-->>User: 200 { final response }
```

---

## Startup Sequence
WbFrodadResrionAdvoatpg*Apst.cst.cs:1))  
  3. Creates distributed application builder.  
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

---c

- [Microsoft Semantic Kernel Agents](https://learn.microsoft.com/en-us/semantic-kernel/frameworks/agent/?pivots=programming-language-csharp)  
- [A2A Project Documentation](https://a2aproject.github.io/A2A/v0.2.5/)  