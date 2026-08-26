# LM Studio Proxy

The **LmStudioProxyController** forwards API requests from the admin server to an underlying LM Studio instance.  This enables the web UI and any external clients to interact with LM Studio through a single, authenticated endpoint.

## Supported Paths
| Path pattern | Method(s) | Description |
|--------------|-----------|-------------|
| `/api/v1/*` or `/v1/*` | All | Forwarded verbatim to `http://localhost:{lmStudioPort}`.  Headers other than `Host`, `Connection`, and `Content-Length` are copied unchanged.
| `/api/v1/models/download/status` | GET/POST? | Special‑case proxy for the LM Studio download status endpoint.

The controller also handles timeouts (5 min) and graceful error responses when the LM Studio service is unreachable.

## Authentication
Proxy requests inherit the same authentication scheme as other API endpoints: a Bearer token in the `Authorization` header or a cookie named `token`.  If no token is present, a **403 Forbidden** response is returned.

## Error Handling
| HTTP status | Reason |
|-------------|--------|
| `502 Bad Gateway` | LM Studio unreachable or responded with non‑2xx status. |
| `500 Internal Server Error` | Unexpected exception while forwarding. |
