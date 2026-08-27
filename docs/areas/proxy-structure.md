# LM Studio Proxy

The **LmStudioProxyController** forwards API requests from the admin server to an underlying LM Studio instance.  This enables the web UI and any external clients to interact with LM Studio through a single, authenticated endpoint.

## Supported Paths
| Path pattern | Method(s) | Description |
|--------------|-----------|-------------|
| `/api/v1/*` or `/v1/*` | All | Forwarded verbatim to `http://localhost:{lmStudioPort}`.  Headers other than `Host`, `Connection`, and `Content-Length` are copied unchanged.
| `/api/v1/models/download/status` | GET/POST? | Special‑case proxy for the LM Studio download status endpoint.

The controller also handles timeouts (5 min) and graceful error responses when the LM Studio service is unreachable.

## Authentication
Proxy requests inherit the same authentication scheme as other API endpoints: a Bearer token in the `Authorization` header or a cookie named `token`.  If no token is present, a **401 Unauthorized** response is returned.

## Model Loading
When a proxied request contains a JSON body with a `model` field, the proxy will:
1. **Unload all currently loaded models** via `lms unload --all`.  This guarantees that only one model instance is active.
2. Load the requested model using `lms load <name>`.
3. Persist the model name in the server configuration (`config.json` field `LmStudioLoadedModel`).  This value is used for UI status displays but is not relied upon to determine runtime state.

The new logic eliminates duplicate loading and ensures consistent state even after manual unloads.

## Error Handling
| HTTP status | Reason |
|-------------|--------|
| `502 Bad Gateway` | LM Studio unreachable or responded with non‑2xx status. |
| `500 Internal Server Error` | Unexpected exception while forwarding. |
