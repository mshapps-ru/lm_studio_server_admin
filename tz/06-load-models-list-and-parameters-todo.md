# To‑Do List for Model List & Parameter Management

[x] **Analysis**
   - Read all docs in `docs/`.
   - Examine source code (Program.cs, LmsCommandExecutor.cs, ConfigManager.cs).
   - Answer Q1–Q5 and document responses.

[x] **Design data structures**
   - Extend `AppConfig` with:
     * `List<ModelInfo> LmStudioModelList`
     * `Dictionary<string, int?> LmStudioModelDefaultLoadParameter`
     * `List<ModelLoadParametersEntry> LmStudioModelLoadParameterList`
   - Define helper classes: `ModelInfo`, `ModelLoadParametersEntry`.

[x] **Update ConfigManager**
   - Ensure serialization/deserialization handles new properties.

[x] **Implement scheduler (5‑minute timer)**
   - Create `Service.ModelListUpdater` with a `System.Threading.Timer`.
   - On tick, fetch `/v1/models` via HttpClient.
   - Parse JSON and compare to current list in config.
   - If changed, write new list atomically to `config.json`.
   - Log errors on failures.

[x] **Parameter merging**
   - Implement method `GetLoadParameters(string modelName)` that merges defaults with overrides.

[x] **Wrap lms load command**
   - Add overload `LmsCommandExecutor.LoadModel(string modelName, Dictionary<string,int?>? parameters = null)`.
   - Construct CLI string: `lms load {model} --context-length <val> --ttl <val>` based on keys.

[x] **Integrate with proxy & startup**
   - Modify `LmStudioProxyController` to call `LoadModelWithParams` using merged params when a model is requested.
   - Update startup logic in Program.cs to use new method if needed.

[x] **Unit tests** (xUnit or MSTest)
   - Test JSON parsing of `/v1/models` response.
   - Test list comparison logic.
   - Validate parameter merging precedence.

[x] **Documentation updates**
   - Update `docs/areas/backend-api-structure.md` to note the new `/v1/models` polling behavior.
   - Add a short section in `README.md` describing how model parameters are configured via `config.json`.

[x] **Commit & review**
    - Ensure all new code compiles.
    - Run existing tests (if any).

---

All tasks above should be reviewed once we confirm answers to Section 3 questions.\n