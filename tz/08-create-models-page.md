# Detailed Plan: Create Models Page

## Overview
The goal is to add a new page in the web UI that lists all LM‑Studio models, their load parameters and default parameters. The page must allow editing of these values and persisting them back into `config.json`.

**Key configuration entries used:**
- `LmStudioModelLoadParameterList`: array of `{ Model: string, Parameters: object }`
- `LmStudioModelDefaultLoadParameter`: object with default params

(The prompt also mentioned an (unused) `LmStudioModelList`; the API will infer available models from the keys in `LmStudioModelLoadParameterList`.)

## Step‑by‑Step Plan

1. **Backend API changes**
   - Add a new controller (`ModelsController`) inheriting from `ApiControllerBase`.  
   - Implement GET `/api/models/list`: reads `config.json`, extracts `LmStudioModelLoadParameterList` and `LmStudioModelDefaultLoadParameter`, returns JSON.
   - Implement POST `/api/models/save-parameters`: accepts payload with updated lists, validates shape (array of objects + default object), writes atomically to `config.json` using `ConfigManager.WriteConfigAsync`.  
   - Guard routes with `[Authorize]` or token check similar to existing settings routes.

2. **Update route registration**
   - In `Startup.cs`/Program.cs add endpoints for the two actions (e.g., `app.MapControllers();`). Ensure they are reachable from UI.

3. **Front‑end page creation**
   - Create `wwwroot/models.html` and `wwwroot/js/models.js`.  
   - Add a navigation link in `index.html` or menu to `/models.html`.
   - On load, fetch `/api/models/list` using `fetch()`; handle auth token via cookie/header.
   - Render a table: columns – **Model Name**, **Load Parameters (JSON)**, **Edit** button, and a separate row for **Default Parameters** with its own Edit button.

4. **Editing UI & form handling**
   - Use a modal dialog or inline form when clicking Edit.  
   - The form contains a `<textarea>` pre‑filled with JSON stringified parameters.  
   - Provide “Save” and “Cancel”. On Save, send POST `/api/models/save-parameters` with updated payload.

5. **Persisting changes to `config.json`**
   - Backend receives new list & default object. Use `ConfigManager.ReadConfigAsync()` → update fields → `ConfigManager.WriteConfigAsync(updated)`.  
   - Implement a file‑lock or use the built‑in async write that overwrites atomically.

6. **Validation & Error handling**
   - Server side: check JSON schema – array items must have `Model` string and `Parameters` object; default must be an object with allowed keys (`contextWindow`, `ttl`, etc.).  
   - Return HTTP 400 with message if invalid.
   - Client side: try‑catch parsing JSON, show errors in modal.

7. **Unit & Integration Tests**
   - Mock `config.json` using a temp file; test GET returns expected structure.  
   - Test POST updates the file and subsequent GET reflects changes.
   - Test invalid payloads return 400.

8. **Documentation updates**
   - Add two new sections to `docs/areas/backend-api-structure.md` describing the endpoints, request/response schemas.
   - Update README or quick‑start if necessary.

9. **Deployment & Verification**
   - Restart application.
   - Visit `/models.html`; verify table populated.
   - Edit a model parameter; click Save; confirm `config.json` changed and page refresh shows new values.
   - Verify default parameters edit works similarly.

10. **Refactor & Clean‑up**
    - Extract common logic (reading/writing config) into a service if needed.
    - Ensure coding standards: use async/await, proper exception handling.
    - Commit with meaningful message.

## Deliverables
- `ModelsController.cs` in the API project.  
- Updated routing configuration.  
- Front‑end files `models.html`, `js/models.js`.  
- Unit tests under `Tests/ModelsControllerTests.cs`.  
- Documentation updates.
