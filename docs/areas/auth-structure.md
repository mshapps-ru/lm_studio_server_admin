# Auth Structure

## Overview
The authentication system is handled by **AuthManager** and integrated into the HTTP server.

### Login
- Endpoint: `POST /api/login`
- Expects JSON body `{ username, password }`.
- On success returns `{ success: true, token }` where **token** is a GUID stored in memory.
- Passwords are hashed with SHA‑256 (Base64). On a successful login, if the stored password is still in plain text, it is immediately replaced by its hash for future logins.

### Logout
- Endpoint: `POST /api/logout`
- Invalidates the session token received from the client.

### Token Validation
- All protected endpoints require an `Authorization: Bearer <token>` header or a cookie named `token`.  If no header is present, the server falls back to reading the `token` cookie.  When the server restarts (e.g., due to a port change), all active sessions are invalidated and clients must re‑authenticate.
- The server validates the token via `AuthManager.TryValidateToken` and rejects unauthenticated requests with **401 Unauthorized**.
- If the stored password is in plain text, it will be hashed on the first successful login so subsequent logins use the hash.

Sessions are kept in memory for 24 h. After expiry they are removed automatically.