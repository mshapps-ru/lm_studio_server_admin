# Backend API Structure

## Overview
The backend exposes a simple REST‑style HTTP API that is served by **HttpServer**. All endpoints are protected with a session token that is created on login and stored in memory.

```
POST   /api/login     – Authenticate user, returns `{ success, token }`
POST   /api/logout    – Invalidate session token, returns { success }
GET    /api/status    – Current status of LM‑Studio Server (running/stopped)
POST   /api/start     – Start the server process
POST   /api/stop      – Stop the server process
GET    /api/settings  – Retrieve current username, port, verbose logging flag, and LM Studio settings
PUT    /api/settings  – Update username (optional), password (optional), port (optional) and verboseLogging (optional)

GET    /api/lmstudio/info         – LM Studio Server info (port, model, connection status)
GET    /api/settings/lmstudio     – LM Studio settings (port, bind address)
POST   /api/settings/lmstudio/detect – Auto-detect LM Studio Server port
PUT    /api/settings/lmstudio     – Update LM Studio settings (port, bind address)
GET    /v1/* /api/v1/*            – Proxy requests to LM Studio Server

**Payload example**:
```
{
    "password": "newPassword",   // optional; plain text. Server will hash it on first login if stored in plain text.
    "port": 8888                  // optional; integer between 1‑65535
}
```
If a field is omitted or empty (e.g., `"password":""`), the existing value remains unchanged.
```

All endpoints that modify state (`/login`, `/logout`, `/start`, `/stop`, `/settings`) expect a JSON body and return JSON. Requests other than `/login` and `/logout` must include an `Authorization: Bearer <token>` header; if not present, the request falls back to checking a cookie named `token`. If neither is supplied, the request is unauthorized. The session token is validated via `AuthManager.TryValidateToken`.

### Status response format
The `/api/status` endpoint returns a JSON object:
```json
{
  "status": "running" | "stopped" | "unknown" | "error",
  "message": "<human‑readable status message>"
}
```
The `status` field is always lower‑cased. The `message` provides a friendly description (e.g., "LM Studio Server is running").

### Authentication Flow
1. Client posts credentials to `/api/login`.
2. If valid, `AuthManager.Login` creates a GUID‑based session token and stores it in `_sessions` with an expiry of 24 h.
3. Token is returned to the client; the client should store it (header or cookie). The token is hashed if the stored password was plain text.
4. Subsequent requests include `Authorization: Bearer <token>` header or a cookie named `token`.
5. `/api/logout` removes the session entry.

### Status Endpoints
- **/api/status** – Calls `LmsCommandExecutor.GetCachedStatus()` and returns status string + message.
- **/api/start**, **/api/stop** – Wrap calls to `LmsCommandExecutor.StartServer()` / `.StopServer()`.

> All responses are JSON with `Content-Type: application/json`.

### LM Studio Info Endpoint
The `/api/lmstudio/info` endpoint returns connection information about the LM Studio Server:
```json
{
  "port": 1234,
  "status": "running" | "stopped" | "unknown" | "error",
  "model": "model-name.gguf" | "N/A" | "No model loaded",
  "connected": true
}
```

### LM Studio Settings Endpoints
**GET /api/settings/lmstudio** returns current settings:
```json
{
  "lmStudioPort": 1234,
  "bindAddress": "0.0.0.0"
}
```

**POST /api/settings/lmstudio/detect** attempts to auto-detect the LM Studio Server port from `lms server status` output:
```json
{
  "success": true,
  "port": 1234
}
```

**PUT /api/settings/lmstudio** updates settings (JSON body):
```json
{
  "lmStudioPort": 1234,
  "bindAddress": "0.0.0.0"
}
```

### Proxy Endpoints
All requests to `/v1/*` and `/api/v1/*` are proxied to the LM Studio Server. Supported paths include:
- `/v1/models`, `/v1/chat/completions`, `/v1/completions`, `/v1/embeddings`
- `/api/v1/models`, `/api/v1/chat`, `/api/v1/models/load`, `/api/v1/models/download`

The proxy forwards the HTTP method, path, headers (except `host`, `connection`, `content-length`), and request body. Timeout is set to 5 minutes for long-running requests (e.g., chat completions).

**Note:** The status detection now correctly handles the 'not running' output from `lms server status`, ensuring that a stopped server returns `status: "stopped"` and `message: "LM Studio Server is stopped"`. 

**Important:** If `/api/settings` changes the listening port, any active sessions are invalidated. Clients must re‑authenticate after a port change.
