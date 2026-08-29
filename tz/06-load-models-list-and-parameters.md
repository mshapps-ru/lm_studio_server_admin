# Plan for Implementing Model List Loading and Parameter Management

## 1. Project Context
The repository is a **LM‑Studio Server Admin** backend written in C#. It exposes several REST endpoints to control the LM‑Studio Server, including proxying `/v1/*` requests. The project already contains configuration files (`config.json`) and a `Program.cs` entry point.

## 2. Initial Analysis Steps
1. **Review the existing documentation** – Inspect all Markdown files under `docs/`. Pay particular attention to sections describing API endpoints, configuration, and how models are loaded via the `lms load` command.
2. **Read the source code** that handles `/v1/*` proxying, scheduled tasks, and configuration parsing (`Program.cs`, `Service/*.cs`).
3. **Identify current config structure** – locate the current `config.json` to understand its schema (currently holds LM‑Studio settings).
4. **Confirm availability of a background scheduler** in the project – look for any timer or scheduled task infrastructure.
5. **Verify logging facilities** – ensure we can write logs when fetching models fails.

## 3. Questions to Clarify
- **Q1:** Does the project already contain a mechanism for periodic tasks (e.g., `System.Threading.Timer` or Hangfire)?
- **Q2:** Where is the LM‑Studio Server API base URL stored? Is it part of configuration or hard‑coded?
- **Q3:** How are command‑line parameters currently passed to `lms load` from within the code? If none, will we need a wrapper around the CLI?
- **Q4:** Are there any security considerations for storing model names and default parameters in plain JSON?
- **Q5:** Is there an existing unit‑test framework (xUnit/MSTest) that we should integrate tests into?

The answers to these will be incorporated into the plan once obtained.

## 4. Data Structures & File Format
1. **`config.json`** will be extended with three new top‑level keys:
   - `LmStudioModelList`: Array of objects `{id, object, owned_by}` fetched from `/v1/models`.
   - `LmStudioModelDefaultLoadParameter`: Object mapping key‑value pairs (e.g., `contextWindow`, `ttl`).
   - `LmStudioModelLoadParameterList`: Array of objects each containing `model` and any overriding parameters.
2. All values will be stored as simple JSON primitives to allow future modifications without code changes.

## 5. API Interaction Logic
- **Endpoint**: `GET /v1/models`
- **Response format**: `{ "data": [ {"id": "gpt-oss-20b", "object": "model", "owned_by": "organization_owner" }, … ] }
- Parse the `data` array and map each entry to the `LmStudioModelList` structure.
- Compare the new list with the current one stored in `config.json`. If identical, skip writing; else overwrite the file.

## 6. Scheduling Mechanism
- Use the existing scheduler (if any) or add a `System.Threading.Timer` that triggers every **5 minutes**.
- On each tick:
   1. Fetch models from `/v1/models`.
   2. If HTTP error or JSON parse failure → log warning, do nothing else.
   3. Otherwise → update config file as described in section 5.

## 7. Error Handling & Logging
- On any exception during fetch/parsing, write a log entry: `Failed to retrieve model list from /v1/models – <error details>`.
- Ensure that no partial writes corrupt `config.json` (write to temp file then atomic rename).

## 8. Default Load Parameters
- Store in `LmStudioModelDefaultLoadParameter` as an object of key‑value pairs.
- Example:
```json
{"contextWindow": 100000, "ttl": 600}
```
- This represents the base parameters for any model unless overridden.

## 9. Individual Model Load Parameters
- Stored in `LmStudioModelLoadParameterList` as an array of objects each containing a mandatory `model` field and optional overrides.
- Example:
```json
[{"model": "gpt-oss-20b", "contextWindow": 120000, "ttl": 1200}]
```
- On startup or when loading a model via CLI wrapper, merge defaults with the override if present; else use defaults.

## 10. Parameter Precedence Logic
1. Load `LmStudioModelDefaultLoadParameter` into a base dictionary.
2. For each individual entry in `LmStudioModelLoadParameterList`, create a merged dictionary where keys from the individual entry overwrite those in the default.
3. When executing `lms load <model>`:
   - If an override exists for `<model>`, use its merged parameters.
   - Otherwise, use the base defaults.

## 11. CLI Wrapper Plan (if needed)
- Provide a small C# helper method that constructs the command string: `lms load {id} --context-length <value> --ttl <value>`.
- Use the parameter dictionary from section 10 to fill values.
- Execute via `Process.Start` with redirected output for logging.

## 12. Testing Strategy
- **Unit tests**:
   - Verify JSON parsing of `/v1/models` response.
   - Compare old vs new model list detection logic.
   - Validate parameter merging precedence.
- **Integration test** (mocked):
   - Simulate a scheduler tick and confirm `config.json` is updated only when changes occur.

## 13. Documentation Updates
- Update `docs/areas/backend-api-structure.md` to note the new `/v1/models` polling behavior.
- Add a short section in `README.md` describing how model parameters are configured via `config.json`.

## 14. Implementation Roadmap (High‑Level)
1. **Analysis** – Complete steps 1–4, answer questions.
2. **Design** – Finalize data structures and schedule integration.
3. **Implement Scheduler & Fetcher** – Add timer, HTTP client logic, JSON handling.
4. **File Write Logic** – Atomic write with temp file.
5. **Parameter Merge Helper** – Create method for defaults + overrides.
6. **CLI Wrapper (if required)** – Wrap `lms load` calls.
7. **Tests** – Add unit and integration tests.
8. **Docs** – Update Markdown files.
9. **Review & Commit** – Ensure all changes are versioned.

## 15. Deliverables
- Updated `config.json` schema documentation in code comments.
- Scheduler implementation in the main program or a dedicated service class.
- Parameter merge utility.
- Tests and updated docs.

---

*All tasks above should be reviewed once we confirm answers to Section 3 questions.*
