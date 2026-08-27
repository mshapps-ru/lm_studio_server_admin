# Refactor Settings Page Plan

## 1. Overview
The current *Settings* page mixes three different concerns into a single form: changing the admin password, updating the application listening port, and configuring LM‑Studio settings (port + bind address). The goal is to split these into **three separate sections** each with its own “Save” button.

### 1.1 Current State
- **UI** – one `<form id="settings-form">` that handles password, port and verbose flag.
- **JS** – `Settings.saveSettings()` validates all fields together and sends a single `/api/settings` PUT request.
- **Backend** – `SettingsController.UpdateSettings` updates username, password, port, and verbose logging in one go.
- **LM‑Studio settings** are handled by a separate `<form id="lmstudio-settings-form">` with its own submit handler but share the same page layout.

### 1.2 Desired Result
| Block | Fields | API endpoint | Save button |
|-------|--------|--------------|-------------|
| **Admin Credentials** | Password + confirm | `/api/settings` (PUT) – send only password field | ✔️ Separate button |
| **Application Port / Verbose** | Port, verbose checkbox | `/api/settings` (PUT) – send port & verbose | ✔️ Separate button |
| **LM‑Studio Settings** | LM‑Studio port + bind address | `/api/settings/lmstudio` (PUT) | ✔️ Separate button |

The password block should not require a port change; the port block should not handle password. Validation and error handling must be isolated.

## 2. Questions & Answers
1. **Should the application automatically reload after changing the listening port?**
   *Answer:* Yes, existing behaviour redirects to the new port after restart. We keep that logic but isolate it within the port block.

2. **Is there a requirement for confirmation dialog before saving changes?**
   *Answer:* Not explicitly; we will keep the current Toast notifications.

3. **Do we need to preserve the existing username field in the UI?**
   *Answer:* Username is not editable on the settings page, so no change required.

4. **Should the LM‑Studio auto‑detect button remain part of the LM‑Studio block?**
   *Answer:* Yes.

5. **Are there any accessibility requirements for new buttons?**
   *Answer:* None mentioned; follow existing patterns.

## 3. Design Decisions
- Create three `<div class="settings-section">` containers in `wwwroot/js/settings.js`. Each will have its own form and submit handler.
- Add separate `<button type="submit" class="save-btn">Save</button>` inside each form.
- In the HTML (not shown but part of `index.html`) we will add these sections; minimal changes to existing markup are acceptable.
- JS: Refactor `Settings.saveSettings()` into three methods:
  - `saveAdminPassword()` – sends `{ password }` only.
  - `savePortAndVerbose()` – sends `{ port, verboseLogging }`.
  - `saveLmStudioSettings()` – unchanged.
- Validation logic for each block is isolated.
- The backend remains largely unchanged; we keep single `/api/settings` endpoint but now the request body contains only relevant fields. `SettingsController.UpdateSettings` already ignores null/zero values, so no changes needed.
- Ensure that redirect after port change still occurs by re‑using existing logic in `savePortAndVerbose()`.

## 4. Implementation Steps
1. **Update HTML** – add three `<form>` blocks with appropriate IDs and fields (password, port, verbose; LM‑Studio port & bind address). Add separate submit buttons.
2. **Update JS** –
   - Split `Settings` object into sections or add new methods as described.
   - Attach event listeners to each form’s submit event.
   - Preserve existing auto‑detect logic inside LM‑Studio block.
3. **Adjust CSS** (if necessary) to style the three blocks consistently.
4. **Verify API Behavior** – ensure that `/api/settings` accepts partial bodies; no changes needed in controller.
5. **Testing** – create unit tests for each new JS method and integration tests for endpoints.
6. **Documentation** – update `docs/areas/backend-api-structure.md` if required to reflect optional fields.

## 5. Deliverables
- Updated front‑end files: `wwwroot/index.html`, `wwwroot/js/settings.js`.
- No backend code changes required, but ensure that the controller correctly ignores missing fields.
- Updated documentation (optional).
- Test cases (if time permits).