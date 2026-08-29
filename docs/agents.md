# Subagents

The project uses two lightweight sub‑agents that are invoked with the `@<name>` syntax. They encapsulate specific browser or documentation maintenance workflows and keep the main agent focused on orchestrating tasks.

| Agent | Description |
|-------|-------------|
| **browser-checker** | Performs all browser inspection work (navigation, screenshots, console logs, DOM inspection, network analysis) by delegating to Chrome DevTools through the `mcp:chrome-devtools` tool. It is the default agent for any request that involves interacting with a web page.
| **docs-maintainer** | Runs after code changes to keep the `docs/` directory in sync with the current source state. It compares project files against existing documentation and updates affected doc files automatically (e.g., adding new API sections, updating configuration snippets). When invoked without a description it auto‑detects drift.

## Invocation
Use the `@<agent>` prefix inside a task string:
```
@browser-checker проверь страницу
@docs-maintainer обнови docs
```
The main agent will forward these calls to the respective sub‑agents via the `subagent` tool.
