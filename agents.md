# VibeSetup UI — Agent Guide

## Browser Work: Delegate to Subagent (default)

**Default rule — delegate to `browser-checker`.** All browser work (navigation, screenshots, DOM inspection, console logs, network requests, performance tracing) should be delegated to the `browser-checker` subagent via the `subagent` tool. This keeps the tool set, reporting format and console/network checks consistent across the project.

**Exception — explicit user override.** If the user **explicitly** asks you to do browser work yourself ("ты сам проверь", "без субагентов", "напрямую", "лично посмотри", "сделай сам" и т. п.), you may call `chrome_devtools_*` tools directly. This exception applies **only** when the request is unambiguous; when in doubt, delegate. When working directly, remember you only see the browser state visible to your own tools — call `list_pages` first if you need to know what is open.

```typescript
// ✅ Default — delegate to subagent
subagent({
  agent: "browser-checker",
  task: "Navigate to http://localhost:7778 and take a screenshot"
})

// ✅ Override — only when user explicitly asked you to do it yourself
//   (e.g. "посмотри сам в браузере", "без субагентов")
chrome_devtools_list_pages({})
chrome_devtools_take_screenshot({})
```

## Subagents

Project-local subagents (stored in `.pi/agents/`). Call with `@<name> <message>`.

| Subagent | Invoke | When to use |
|----------|--------|-------------|
| `browser-checker` | `@browser-checker проверь страницу` | **All browser work (by default)** — navigation, screenshots, console logs, DOM inspection, network analysis. Delegate to this subagent. Direct `chrome_devtools_*` calls are allowed only when the user explicitly asks you to do browser work yourself. |
| `docs-maintainer` | `@docs-maintainer обнови docs` | **After any code changes** — compares code with `docs/` and updates affected doc files. Use with a description of what changed, or without one to auto-detect drift. |

### browser-checker workflow

```text
User: проверь как выглядит публичная страница
Agent: subagent({ agent: "browser-checker", task: "Navigate to http://localhost:7778, take screenshot of public page" })
Subagent: opens browser → takes screenshot → checks console → reports findings
```

### docs-maintainer workflow

```text
User: добавь новый эндпоинт POST /api/users/import
Agent: <implements the endpoint>
User: @docs-maintainer обнови docs
Subagent: reads new code → updates docs/areas/backend-api-structure.md → lists changes
```

Without a description the subagent reads the code itself and compares it with existing docs.

## Common Issues

