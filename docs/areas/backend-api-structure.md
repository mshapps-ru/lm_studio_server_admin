# Backend API Structure

## Overview
The backend exposes a simple REST‑style HTTP API that is served by **HttpServer**. All endpoints are protected with a session token that is created on login and stored in memory.

```
POST   /api/login     – Authenticate user, returns { success, token }
POST   /api/logout    – Invalidate session token, returns { success }
GET    /api/status    – Current status of LM‑Studio Server (running/stopped)
POST   /api/start     – Start the server process
POST   /api/stop      – Stop the server process
GET    /api/settings  – Retrieve current username & port
PUT    /api/settings  – Update username (optional), password (optional) and/or port.

**Payload example**:
```
{
    "password": "newPassword",   // optional; plain text. Server will hash it on first login if stored in plain text.
    "port": 8888                  // optional; integer between 1‑65535
}
```
If a field is omitted or empty (e.g., `"password":""`), the existing value remains unchanged.
```

All endpoints that modify state (`/login`, `/logout`, `/start`, `/stop`, `/settings`) expect a JSON body and return JSON. Requests other than `/login` and `/logout` must include an `Authorization: Bearer <token>` header or a cookie named `session`. The server validates the token via **AuthManager.TryValidateToken**.

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
4. Subsequent requests include `Authorization: Bearer <token>` header or a `session=<token>` cookie.
5. `/api/logout` removes the session entry.

### Status Endpoints
- **/api/status** – Calls `LmsCommandExecutor.GetCachedStatus()` and returns status string + message.
- **/api/start**, **/api/stop** – Wrap calls to `LmsCommandExecutor.StartServer()` / `.StopServer()`.

> All responses are JSON with `Content-Type: application/json`.

**Note:** The status detection now correctly handles the 'not running' output from `lms server status`, ensuring that a stopped server returns `status: "stopped"` and `message: "LM Studio Server is stopped"`. 

**Important:** If `/api/settings` changes the listening port, any active sessions are invalidated. Clients must re‑authenticate after a port change.
