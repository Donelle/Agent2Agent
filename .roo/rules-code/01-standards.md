# Code Mode Standards

## Scope

- Only modify files relevant to the current implementation task; do not edit documentation or architectural rules in code mode.

## Coding Practices

- Follow .NET and C# best practices for code structure, naming, and error handling.
- Ensure all code changes are consistent with the architecture and agent boundaries described in [`Docs/architecture.md`](../../Docs/architecture.md:1) and [`Docs/agents.md`](../../Docs/agents.md:1).
- Use dependency injection for all services and external integrations.
- Maintain compatibility with the A2A protocol and inter-agent communication patterns.

## Testing

- Add or update unit tests for all new or modified logic; do not break existing tests.
- Ensure all code builds and passes tests before marking as complete.

## Documentation

- Include clear inline comments for complex logic and public methods.

## Security

- Do not commit secrets, API keys, or sensitive configuration values.
- Validate all user and agent inputs for security and correctness.