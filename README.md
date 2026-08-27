# LM Studio Server Admin

> A lightweight console/Windows‑Service application written in C# that provides an HTTP interface for managing **LM Studio** (start/stop/status) and editing its configuration.

---

## 1. Project Summary

* **Language & Runtime:** C# (.NET 10+) – compiled to a single executable.
* **Modes of operation**
  * Console application – runs in the terminal.
  * Windows Service – started with the `-service` command‑line flag.
* **HTTP Port** – listens on port **7778** (configurable via `config.json`).
* **UI** – simple single‑page app (SPA) served from `/wwwroot`. After successful login it shows two tabs:
  * **Home** – status panel + *Start/Stop* buttons + LM Studio connection info (port, model, connected status).
  * **Settings** – edit admin credentials, listening port, and LM Studio Server settings (port, bind address) with auto-detect.
* **API Proxy** – forwards `/v1/*` and `/api/v1/*` requests to LM Studio Server for remote access.
* **Authentication** – token based session stored in a cookie or `Authorization` header. Passwords are hashed with SHA‑256 on first login.
* **LM Studio Interaction** – commands executed through the external CLI (`lms server start/stop/status`). Status is cached and refreshed every minute.
* **Configuration** – persisted in `config.json` located next to the executable.

---

## 2. Features

| Feature | Description |
|---------|-------------|
| Console & Service | Same binary works both as a console app and a Windows service (`-service`). |
| HTTP Interface | Simple `HttpListener` based server – no heavy framework required. |
| Login/Logout | Token is issued on `/api/login`, invalidated by `/api/logout`. |
| Home Tab | Shows current LM Studio status, Start & Stop buttons, and LM Studio connection info (port, model, connected status).
| Settings Tab | Edit admin credentials and listening port; changes are written to `config.json` and trigger a server restart if the port changes.
+| Verbose Logging | Toggle in Settings to enable detailed proxy request/response logs. |
| LM Studio Settings | Configure LM Studio Server port and bind address; auto-detect port from `lms server status`. |
| API Proxy | Proxies `/v1/*` and `/api/v1/*` requests to LM Studio Server for remote access.
| Automatic Status Refresh | Background task polls LM Studio every 60 s. |
| Logging | File logger (`logs/app.log`) with INFO/WARN/ERROR levels. |
| Session Invalidation | Changing the listening port invalidates existing sessions; users must log in again. |

---

## 3. Architecture Overview

```
LmStudioServerAdmin/
├── LmStudioServerAdmin.csproj          # Project file
├── Program.cs                          # Entry point & mode detection
├── Config/
│   ├── AppConfig.cs                    # Settings model
│   └── ConfigManager.cs                # Load / save config.json
├── Logging/
│   └── Logger.cs                       # Simple file logger
├── Server/
│   ├── HttpServer.cs                  # HTTP listener, routing & static files
│   ├── AuthManager.cs                 # Session handling + password hashing
│   ├── HomeController.cs              # /api/status, /api/start/stop, /api/lmstudio/info
│   ├── SettingsController.cs          # /api/settings GET/PUT, /api/settings/lmstudio
│   └── LmStudioProxyController.cs     # Proxies /v1/* and /api/v1/* to LM Studio Server
├── Commands/
│   └── LmsCommandExecutor.cs         # Executes LM Studio CLI commands
├── Service/
│   └── StatusChecker.cs              # Background status polling (optional)
└── wwwroot/                           # SPA assets (index.html, css, js)
```

---

## 4. Setup & Build

```bash
# Clone the repo and navigate to the project folder
git clone <repo-url>
cd LmStudioServerAdmin

# Restore NuGet packages and build
dotnet restore

dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true


Сборка и старт в терминале:
taskkill /F /IM LmStudioServerAdmin.exe; dotnet build; dotnet run
Запуск хрома с дебагом:
chrome.exe --remote-debugging-port=9222 
```

The resulting executable (`LmStudioServerAdmin.exe`) will be placed under `bin/Release/net10.0/win-x64/publish`.

---

## 5. Running the Application

### Console Mode (default)
```bash
./LmStudioServerAdmin.exe          # or LmStudioServerAdmin.exe on Windows
```
The app will start an HTTP server on port **7778** and wait for user input to terminate.

### Service Mode
```bash
# From the same directory as the executable
LmStudioServerAdmin.exe -service
```
This registers the binary as a Windows service (requires admin rights). You can then manage it via `services.msc` or PowerShell:
```powershell
Start-Service -Name "LmStudioServerAdmin"
Stop-Service -Name "LmStudioServerAdmin"
```

---

## 6. Usage in Browser

1. Open `http://localhost:7778/`. You’ll see a login screen.
2. Use credentials from `config.json` (default *admin/admin*). After successful login you’ll be redirected to the main UI with **Home** and **Settings** tabs.
3. **Home:** view LM Studio status, click **Start** or **Stop** buttons, see LM Studio connection info (port, model, status).
4. **Settings:** edit admin credentials, port, and LM Studio Server settings; press **Save**.
5. Click **Exit** to logout (clears the token and returns to login).

---

## 7. Firewall Configuration

By default Windows Firewall blocks inbound connections. To allow access from other machines on the network, open port **7778**:

### PowerShell (admin)
```powershell
New-NetFirewallRule -DisplayName "LM Studio Admin Port 7778" -Direction Inbound -LocalPort 7778 -Protocol TCP -Action Allow
```

### GUI
1. **Control Panel** → **Windows Defender Firewall** → **Advanced settings**
2. **Inbound Rules** → **New Rule**
3. **Port** → **TCP** → Local port: `7778`
4. **Allow the connection** → check all profiles → Name: `LM Studio Admin 7778`

After adding the rule, the admin panel will be accessible from other machines:
```
http://<your-IP>:7778
```

---

## 8. Configuration File (`config.json`)

```json
{
    "username": "admin",
    "password": "admin",   // will be hashed on first successful login
    "port": 7778,
    "lmStudioPort": 1234,
    "bindAddress": "0.0.0.0"
}
```

| Field | Description |
|-------|-------------|
| `username` | Admin username |
| `password` | Plain text password — hashed on first login |
| `port` | Admin server listening port |
| `lmStudioPort` | LM Studio Server port |
| `bindAddress` | Bind address (`0.0.0.0` for all interfaces, `localhost` for local only) |
+| `verboseLogging` | Boolean flag enabling verbose proxy logging (default false) |

The file is created automatically if missing and is overwritten with the hashed password after a successful login.

---

## 9. Extending & Debugging

* **HTTPS** – replace `HttpListener` with ASP.NET Core/Kestrel for TLS support.
* **Persisted Sessions** – move session store from memory to a file or database for service restarts.
* **Unit Tests** – add tests for `AuthManager`, `ConfigManager`, and HTTP endpoints using an in‑memory server or `HttpClient`.

---

## 10. License & Credits

This project is open source under the MIT license. It uses only standard .NET libraries (`System.Net.HttpListener`, `Newtonsoft.Json`) and no external dependencies beyond those bundled with the SDK.
