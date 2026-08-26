# Project Overview

This repository implements a lightweight **LM Studio Server Admin** utility. It exposes an HTTP API that allows controlling the LM‑Studio server process, retrieving its status, and configuring connection parameters.

## Core Components
- **HttpServer** – Wraps `System.Net.HttpListener` to handle REST endpoints.
- **AuthManager** – In‑memory session token management using SHA‑256 hashed passwords. The service issues a Bearer token on login that can be sent in the `Authorization` header or stored in a cookie named `token`.
- **ConfigManager** – Persist configuration (username, password hash, port, LM Studio port, bind address) in `config.json` located next to the executable.
- **LmsCommandExecutor** – Executes external commands such as starting or stopping LM‑Studio via its command line interface. Auto-detects the LM Studio Server port from `lms server status` output.
- **LmStudioProxyController** – Proxies API requests (`/v1/*`, `/api/v1/*`) to the LM Studio Server, enabling remote access through the admin panel.

## Typical Usage
1. Run the application in console mode (`dotnet run`) or install as a Windows Service.
2. Open the web UI at `http://localhost:{port}` (default 7778, configurable via `bindAddress`).
3. Log in with admin credentials, then control the server process and view LM Studio connection info.
4. Configure LM Studio Server port in Settings or use Auto-detect.
5. Use the built-in proxy to access LM Studio API endpoints (`/v1/chat/completions`, etc.) through the admin server.
6. Changing the listening port triggers a restart of the HTTP service; any active sessions become invalid and must re‑authenticate.
