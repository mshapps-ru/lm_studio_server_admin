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
  * **Home** – status panel + *Start/Stop* buttons.
  * **Settings** – edit username, password and listening port.
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
| Home Tab | Shows current LM Studio status, Start & Stop buttons.
| Settings Tab | Edit admin credentials and listening port; changes are written to `config.json`.
| Automatic Status Refresh | Background task polls LM Studio every 60 s.
| Logging | File logger (`logs/app.log`) with INFO/WARN/ERROR levels. |

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
│   ├── HomeController.cs              # /api/status, /api/start/stop
│   └── SettingsController.cs          # /api/settings GET/PUT
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
3. **Home:** view LM Studio status, click **Start** or **Stop** buttons.
4. **Settings:** edit username, password, port; press **Save**.
5. Click **Exit** to logout (clears the token and returns to login).

---

## 7. Configuration File (`config.json`)

```json
{
    "username": "admin",
    "password": "admin",   // will be hashed on first successful login
    "port": 7778
}
```

The file is created automatically if missing and is overwritten with the hashed password after a successful login.

---

## 8. Extending & Debugging

* **HTTPS** – replace `HttpListener` with ASP.NET Core/Kestrel for TLS support.
* **Persisted Sessions** – move session store from memory to a file or database for service restarts.
* **Unit Tests** – add tests for `AuthManager`, `ConfigManager`, and HTTP endpoints using an in‑memory server or `HttpClient`.

---

## 9. License & Credits

This project is open source under the MIT license. It uses only standard .NET libraries (`System.Net.HttpListener`, `Newtonsoft.Json`) and no external dependencies beyond those bundled with the SDK.
