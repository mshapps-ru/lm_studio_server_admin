# Auth Structure

## Overview
The authentication system is handled by **AuthManager** and integrated into the HTTP server.

### Login
- Endpoint: `POST /api/login`
- Expects JSON body `{ username, password }`.
- On success returns `{ success: true, token }` where **token** is a GUID stored in memory.
- Passwords are hashed with SHA‑256 (Base64). If the stored password was plain text it is updated to hash after successful login.

### Logout
- Endpoint: `POST /api/logout`
- Invalidates the session token received from the client.

### Token Validation
- All protected endpoints require an `Authorization: Bearer <token>` header or a cookie named `session`.
- The server validates the token via `AuthManager.TryValidateToken` and rejects unauthenticated requests with **401 Unauthorized**.

Sessions are kept in memory for 24 h. After expiry they are removed automatically.