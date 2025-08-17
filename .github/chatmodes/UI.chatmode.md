---
description: 'User Interface chatmode for building UI components'
tools: [ 'filesystem', 'json-mcp-server', 'mcp-mermaid' , 'memory', 'sequential-thinking', 'fetch', 'codebase']
model: 'GPT-4o'
---

# User Interface mode instructions


The Agent2Agent WebFrontend is a Blazor Server application that serves as the primary user interface for interacting with the multi-agent system. Its purpose is to provide a seamless, intuitive, and responsive experience for users seeking information or assistance related to vehicle registration.

## Intended Purpose

- Enable users to submit queries and receive responses powered by the RegistrationAdvocateAgent and supporting agents.
- Present agent responses in a clear, conversational format, highlighting relevant knowledge and sources.
- Support fallback and enrichment logic transparently, so users always receive the best available answer.
- Facilitate exploration of vehicle registration topics through guided prompts or suggestions.

## User Experience Focus

- Prioritize clarity, accessibility, and ease of use for all user interactions.
- Ensure fast feedback and visible progress indicators during agent processing.
- Maintain a consistent, modern design aligned with best practices for web accessibility.
- Clearly indicate when responses are AI-generated and cite sources when available.
- Provide error handling and helpful messages for failed or incomplete agent responses.
- Support extensibility for future features such as chat history, agent selection, or advanced search.

## Implementation Details

- Utilize Blazor components for building the UI, ensuring reusability and maintainability.
- Leverage dependency injection for services like chat history and agent communication.
- Implement state management to handle user sessions and message threads effectively.
- When adding client side functionality use Typescript 

## API References

- [Blazor WebAssembly (.NET 8)](https://docs.blazorbootstrap.com/getting-started/blazor-webassembly-net-8): Official documentation for building Blazor WebAssembly apps with .NET 8, including setup and project structure.
- [Blazor Layout Setup](https://docs.blazorbootstrap.com/layout/blazor-webassembly): Guide to configuring layouts and navigation in Blazor applications.
- [Blazor Components](https://demos.blazorbootstrap.com/): Interactive demos and documentation for reusable Blazor UI components.
- [Bootstrap](https://getbootstrap.com/docs/5.3/getting-started/introduction/): Reference for Bootstrap 5.3, the CSS framework used for responsive design and UI consistency.
