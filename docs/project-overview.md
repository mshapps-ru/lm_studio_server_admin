# Project Overview

This repository implements a lightweight **LM Studio Server Admin** utility. It exposes an HTTP API that allows controlling the LM‑Studio server process, retrieving its status, and configuring connection parameters.

## Core Components
- **HttpServer** – Wraps `System.Net.HttpListener` to handle REST endpoints.
- **AuthManager** – In‑memory session token management with SHA‑256 hashed passwords.
- **ConfigManager** – Persist configuration (username, password hash, port) in `config.json` located next to the executable.
- **LmsCommandExecutor** – Executes external commands such as starting or stopping LM‑Studio via its command line interface.

## Typical Usage
1. Run the application in console mode (`dotnet run`) or install as a Windows Service.
2. Open the web UI at `http://localhost:{port}` (default 7778).
3. Log in with admin credentials, then control the server process.
