# Project Plan – Adding Verbose Logging Switcher

## Overview
The goal is to expose a toggle in the **Settings** tab that controls the `verboseLogging` boolean in `AppConfig`. The value must be persisted in `config.json` and kept in memory for runtime usage.

## Steps
1. **Review current architecture** (docs, code) – confirm where config is loaded/saved, how settings are served, and UI load flow.
2. **Identify API surface changes** – decide whether to extend existing `/api/settings` response or create a dedicated endpoint for the verbose flag.
3. **Update `AppConfig`** – already contains `verboseLogging`; no change needed except ensuring it is serialized.
4. **Expose value via HTTP** – modify `SettingsController.GetSettings` to include `verboseLogging` in its JSON response, and ensure `PUT /api/settings` accepts this field.
5. **Add UI toggle** – create a switch element in the Settings tab (e.g., `<input type="checkbox" id="verbose-logging">`).
6. **Load current value** – update `Settings.init()` to fetch settings and set the checkbox state.
7. **Persist changes** – modify `saveSettings()` to include the flag when sending PUT request, then call API endpoint; handle response.
8. **Ensure runtime usage** – verify that any component reading `AppConfig.verboseLogging` (e.g., proxy controller) behaves correctly; add a comment if further integration required.
9. **Unit / Integration tests** – optional but recommended: test that flag is persisted and read back.
10. **Documentation** – update README or docs to mention new setting.

## Questions for the User
- Should the verbose flag be part of the same `/api/settings` payload, or do we need a dedicated endpoint?
- Do you want immediate effect on existing connections (e.g., log all proxy traffic) after toggling, or only for subsequent requests?
- Are there any constraints on how the UI should indicate the current state (checked/unchecked, label text)?

## Deliverables
1. Updated **SettingsController** (GET & PUT).
2. Modified **settings.js** to include switcher.
3. Updated HTML/CSS if necessary for toggle styling.
4. Any additional code needed to read the flag at runtime.
5. Test cases (if applicable).
6. Documentation updates.

## TODO List
See `tz/03-create-switcher-for-logging-todo.md` for a detailed task list.
