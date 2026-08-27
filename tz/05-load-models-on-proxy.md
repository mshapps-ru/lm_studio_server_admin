# Load‑Models‑on‑Proxy Plan

## 1. Context & Scope
The goal is to add automatic model loading when a request reaches the LM Studio proxy while no model (or a different model) is currently loaded.

* **Where we persist state** – `config.json` via the `AppConfig.LmStudioLoadedModel` field.
* **When we trigger load** – on every proxied request that contains a `model` field in its JSON body and the target LM Studio instance does not already have that model loaded.

## 2. Current State Review
The repository already:
- Exposes proxy routes (`LmStudioProxyController`).
- Calls `LmsCommandExecutor.GetStatus()`, `GetLmStudioPort()` and `GetLoadedModels()` to display current status in the UI.
- Persists configuration via `ConfigManager.Load/Save` (but no field for the loaded model).

## 3. Questions & Answers
1. **What is the exact command to load a model?**
   - *Answer:* `lms load <model‑name>` – this matches existing CLI usage.
2. **Do we need to pass any additional arguments (e.g., quantization, GPU)?**
   - *Answer:* No; default parameters are acceptable for now.
3. **Where should the loaded model name be stored once a load succeeds?**
   - *Answer:* In `config.json` under property `LmStudioLoadedModel`. An empty string denotes “no model loaded”.
4. **Should we reload the same model if it is already loaded?**
   - *Answer:* No; skip loading if the target name matches the persisted value.
5. **What happens on application start when a model is recorded in config?**
   - *Answer:* The server should attempt to load that model automatically (this will be added during initialization).

## 4. Implementation Steps
1. **Extend configuration**
   - Add `public string LmStudioLoadedModel { get; set; } = "";` to `AppConfig`.
2. **Persist new field** – no code changes needed; `JsonSerializer` will serialize it automatically.
3. **Add a helper in `LmsCommandExecutor`**
   - Method `public static void LoadModel(string modelName)` that runs `lms load <modelName>`, updates caches, and returns success status.
4. **Update `LmStudioProxyController.HandleProxyRequest`**
   - After reading request body, if the method is POST/PUT and body contains a JSON key `model`, extract its value.
   - Load current config via `ConfigManager.Load()`.
   - If extracted model differs from `config.LmStudioLoadedModel`, call `LmsCommandExecutor.LoadModel(model)`.
   - On success, update `config.LmStudioLoadedModel` and persist with `ConfigManager.Save(config)`.
5. **Auto‑load on startup**
   - In the entry point (e.g., `Program.cs`) or an initialization method, after loading config, check if `LmStudioLoadedModel` is non-empty; if so, call `LmsCommandExecutor.LoadModel(...)` to ensure consistency.
6. **Adjust UI (optional)** – The home endpoint already shows loaded models via `GetLoadedModels()`. No change required unless we want to show persisted field directly.

## 5. Testing Plan
- Unit test `LoadModel` by mocking the process start and ensuring caches update.
- Integration test: Start server, send a proxy POST with `{ "model": "gpt-oss-20b" }`, verify that the command was executed and config file contains the new value.
- Regression test: If no `model` field is present or same model already loaded, no reload occurs.

## 6. Deliverables
- Updated `AppConfig.cs` with new property.
- New method in `LmsCommandExecutor`.
- Modified proxy controller to trigger load.
- Optional startup logic for auto‑loading on launch.
- Documentation updates (none required beyond plan).

---
**All items from the original prompt are included and addressed.**