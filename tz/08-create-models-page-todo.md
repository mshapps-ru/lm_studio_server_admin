- **Step 1:** Add new Models tab in `wwwroot/index.html` between Home and Settings.
- **Step 2:** Create a dedicated JavaScript module `wwwroot/js/models.js` that:
    - Fetches the model list from `/api/models`.
    - Displays models in a table with columns: ID, Object, Owned_by, Current Parameters.
    - Provides an "Update list" button that forces a refresh of the list.
    - For each model row add editable fields for parameters defined in `LmStudioModelLoadParameterList` and a save button to persist changes via `/api/models/{model}/override`.
- **Step 3:** Add UI controls to edit default parameters:
    - A separate section listing default key/value pairs from `LmStudioModelDefaultLoadParameter`.
    - Editable inputs and a "Save defaults" button that POSTs to `/api/models/default`.
- **Step 4:** Backend API changes:
    - Create `Server/ModelsController.cs` with the following endpoints:
        1. `GET /api/models` – returns `{ models: [...] }` from `config.LmStudioModelList`.
        2. `POST /api/models/default` – accepts JSON body of default parameters, updates `LmStudioModelDefaultLoadParameter`, saves config.
        3. `PUT /api/models/{model}/override` – accepts JSON body of key/value overrides for the specified model, updates `config.LmStudioModelLoadParameterList`, saves config.
    - Update `HttpServer.HandleRequest` to route these paths to `ModelsController` methods.
- **Step 5:** Ensure authentication checks (same as other API endpoints). All new routes should be under `/api/` and require a valid auth token.
- **Step 6:** Add minimal CSS styling in `style.css` for the Models page layout.
- **Step 7:** Write unit tests (if applicable) for ModelsController methods to verify JSON serialization/deserialization and config persistence.
- **Step 8:** Run integration test by starting server, navigating to `/`, logging in, clicking new "Models" tab, verifying list display and ability to edit parameters.
- **Step 9:** Commit changes with descriptive commit message.
