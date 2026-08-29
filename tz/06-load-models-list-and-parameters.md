# Load Models List and Parameters Plan

## 1. Context & Goals
The task is to extend the **LM Studio Server Admin** project so that:

* The list of available models from the LM‑Studio `/v1/models` endpoint is fetched automatically every 5 minutes.
* That list is persisted in `config.json` under the key `LmStudioModelList`.
* Default load parameters (context window, ttl, …) are stored under `LmStudioModelDefaultLoadParameter`.
* Individual model‑specific load parameters may override defaults and are stored under `LmStudioModelLoadParameterList`.
* All changes to the config file must be applied only when the data actually changed; errors during fetch should be logged but ignored.

The plan below walks through analysis, questions, design decisions, implementation steps, testing strategy and a final To‑Do list.

## 2. Current Understanding (from docs & code)
* **`config.json`** is loaded/saved by `ConfigManager`.  It already contains a field for the currently *loaded* model (`LmStudioLoadedModel`).
* **Proxy controller** already parses request bodies for a `model` key and calls `LmsCommandExecutor.LoadModel()`.
* **`AppConfig`** serializes to JSON automatically – adding new properties is straightforward.
* No scheduled background task exists; the only recurring job is the proxy handling.

### Gaps that need clarification
1. Exact shape of the `/v1/models` response.
2. Whether the list should include *all* objects returned by `/v1/models` or a filtered subset.
3. How to detect “changed” – a simple JSON string comparison will suffice for now.
4. Where in the code base to wire the 5‑minute timer (e.g., `Program.cs`, a dedicated background service). 

## 3. Questions & Answers (to be refined by user)
| # | Question | Current answer / assumption |
|---|----------|----------------------------|
|1| What fields are present in the `/v1/models` JSON? | Likely an array of `{id, object, owned_by}` similar to OpenAI API.
|2| Do we need to handle pagination or large lists? | Assume single request returns full list; if needed later add paging logic.
|3| Should the timer run even if the server is stopped? | No – only when the HTTP service is running.
|4| Where should errors be logged? | Use `Logger.Error` in the fetch method, no crash.

## 4. Design Decisions
* **Background task**: create a static async `ModelUpdater.StartAsync()` that schedules a `Timer`. It will run every 5 min and invoke `UpdateModelsListAsync()`.
* **Fetching**: use `HttpClient` to GET `http://localhost:{LmStudioPort}/v1/models`.
* **Deserialization**: map JSON array directly into `ModelInfo[]` (class defined in a new file).
* **Change detection**: keep the previous JSON string; compare with new one. If identical, skip write.
* **Config structure**: add properties to `AppConfig` and ensure serialization handles them.
* **Load parameters**: similar approach – expose two lists in config and use them when executing a load command.

## 5. Implementation Steps (in order)
1. **Define model DTOs** (`ModelInfo`, `DefaultLoadParameter`, `IndividualLoadParameter`).
2. **Extend `AppConfig`** to include:
   ```csharp
   public List<ModelInfo> LmStudioModelList { get; set; } = new();
   public List<DefaultLoadParameter> LmStudioModelDefaultLoadParameter { get; set; } = new();
   public List<IndividualLoadParameter> LmStudioModelLoadParameterList { get; set; } = new();
   ```
3. **Add a static `ModelUpdater` class** in a suitable namespace (e.g., `LmStudioServerAdmin.Background`).  It will:
   * Accept the current `AppConfig` and an `ILogger`.
   * Use a `Timer` to trigger every 5 min.
   * In each tick, call `FetchModelsAsync()`, compare JSON, update config if changed.
4. **Implement `FetchModelsAsync()`**:
   * Build URL using config's `LmStudioPort`.
   * Perform GET; on non‑200 status log error and return null.
   * Deserialize to `ModelInfo[]`.
5. **Write back to config** when changed – call `ConfigManager.Save(config)`.
6. **Hook into startup** (`Program.cs`) after loading config: `ModelUpdater.StartAsync(config, Logger.Instance);`
7. **Implement parameter resolution logic** in `LmsCommandExecutor.LoadModel()`:
   * Build command arguments from defaults + overrides (individual list takes precedence).
   * Pass them to the external CLI.
8. **Unit Tests**:
   * Mock HTTP responses for `/v1/models` and verify config updates.
   * Test that unchanged lists do not trigger a write.
9. **Integration test**: start server, let timer run twice, ensure config contains expected models.
10. **Documentation update** – add section to `docs/areas/backend-api-structure.md` explaining the new background task.

## 6. Risks & Mitigations
| Risk | Mitigation |
|------|------------|
| Timer leaks on application exit | Dispose timer in `AppDomain.ProcessExit` handler.
| Config file corruption from concurrent writes | Use a single thread (Timer callback serialises). 
| Network failures | Log and ignore; retry next cycle.

## 7. Deliverables
* Updated `AppConfig.cs` with new properties.
* New DTO classes in `/src/Models/ModelInfo.cs` (or similar).
* Background service class `ModelUpdater.cs`.
* Extension to `LmsCommandExecutor.LoadModel()` for parameter handling.
* Unit tests under `/tests`.
* Updated docs and README.

## 8. Next Steps
1. Clarify questions above.
2. Once confirmed, start implementing the DTOs and config updates.
3. Then proceed to the background updater.
4. Finally, integrate parameter logic into load command.
5. Write tests and update documentation.

---
**Note:** This plan assumes a straightforward JSON array from `/v1/models`.  If the real shape differs, adjust DTOs accordingly.
