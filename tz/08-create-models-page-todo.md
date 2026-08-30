# To‑Do List: Create Models Page

## ✅ 1. API Design & Backend Logic
- **GET** `/api/models/list` – Return the current list of models, their load parameters, and default load parameters.
- **POST** `/api/models/save-parameters` – Accept a payload with updated `LmStudioModelLoadParameterList` and/or `LmStudioModelDefaultLoadParameter`. Validate, write to `config.json`, and return success status.

## ✅ 2. Route Registration
- Add the new endpoints in the appropriate controller (e.g., `SettingsController`).
- Ensure that only authenticated users can access these routes.

## ✅ 3. Front‑end Page Creation
- Create a new page under `wwwroot/` – e.g., `models.html` and corresponding JS (`models.js`).
- Add navigation link from the main menu.
- Use fetch API to call `/api/models/list` on load.
- Render a table or list showing:
  - Model name
  - Current load parameters
  - Edit button for each model
  - Button to edit default parameters

## ✅ 4. Editing UI & Form Handling
- For each model, provide an inline form or modal dialog that allows the user to modify the JSON parameters.
- Provide a separate form for editing `LmStudioModelDefaultLoadParameter`.
- Validate input on the client side before sending.

## ✅ 5. Persisting Changes to `config.json`
- Backend receives POST, validates structure and types, then writes atomically back to `config.json` using `ConfigManager` or direct file write.
- Ensure that concurrent edits are handled (e.g., by locking the file during write).

## ✅ 6. Input Validation & Error Handling
- Server‑side: JSON schema validation for both lists and default parameters.
- Client‑side: Show user-friendly error messages if validation fails.

## ✅ 7. Unit / Integration Tests
- Mock `config.json` and test GET returns correct data.
- Test POST with valid data updates the file correctly; test invalid data returns appropriate errors.

## ✅ 8. Documentation Updates
- Add a section in `docs/areas/backend-api-structure.md` explaining the new endpoints.
- Update any relevant README or quick‑start docs.

## ✅ 9. Deployment & Verification
- Restart the server.
- Navigate to `/models.html`, confirm data loads.
- Edit parameters, save, and verify that `config.json` is updated and changes persist after restart.

## ✅ 10. Refactor & Clean‑up
- Extract reusable logic into services if needed.
- Ensure code follows project coding standards.
- Commit all changes with appropriate commit messages.
