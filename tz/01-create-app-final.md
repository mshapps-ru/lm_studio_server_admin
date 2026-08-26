# ТЗ: Приложение администрирования LM Studio Server (Windows Service)

## 1. Общие сведения

| Параметр | Значение |
|-----------|----------|
| **Язык** | C# (.NET 10+) |
| **Тип приложения** | Консольное приложение, способно работать как служба Windows |
| **Режим службы** | Запуск с параметром `-service` |
| **HTTP‑порт** | 7778 (по умолчанию, настраиваемый в `config.json`) |
| **Файл настроек** | `config.json` рядом с исполняемым файлом |
| **Управление LM Studio** | Команды `lms server start/stop/status` через `Process.Start` |
| **Сессии** | Токен‑база, хранящийся в памяти; при смене порта все активные токены удаляются |

---

## 2. Архитектура

```
LmStudioServerAdmin/
├── LmStudioServerAdmin.csproj          # Проект
├── Program.cs                          # Точка входа: console или service
├── Config/
│   ├── AppConfig.cs                    # Модель настроек (Username, Password, Port)
│   └── ConfigManager.cs                # Чтение/запись config.json + хеширование пароля
├── Logging/
│   └── Logger.cs                       # Файловый логгер (INFO/WARN/ERROR)
├── Server/
│   ├── HttpServer.cs                   # HttpListener, маршрутизация статических файлов и API
│   ├── AuthManager.cs                  # Авторизация + хранение токенов
│   ├── HomeController.cs               # /api/status, /api/start, /api/stop
│   └── SettingsController.cs           # /api/settings GET/PUT
├── Commands/
│   └── LmsCommandExecutor.cs          # Выполнение команд lms
├── Service/
│   ├── StatusChecker.cs               # Таймер проверки статуса LM Studio (60 с)
│   └── WindowsService.cs              # Реализация службы Windows
└── wwwroot/                            # SPA: index.html, css/, js/
```

---

## 3. Этапы разработки (обновлённый план)

### Этап 1: Инициализация проекта
1. Создать solution и консольный проект `LmStudioServerAdmin`.
2. Настроить `.csproj`: target‑framework `net10.0`, `publishSingleFile=true`.
3. Добавить папки согласно архитектуре.
4. Установить NuGet:
   * `Microsoft.Extensions.Hosting.WindowsServices`
   * `Newtonsoft.Json` (или `System.Text.Json`).
5. Реализовать `AppConfig.cs` и `ConfigManager.cs` (чтение/запись `config.json`, хеширование пароля SHA‑256).
6. В `Program.cs` определить режим запуска (`-service` → `ServiceBase.Run(new WindowsService())`; иначе консольное приложение).
7. При первом запуске создать файл `config.json` с дефолтными данными.

### Этап 2: HTTP‑сервер и авторизация
1. Реализовать `HttpServer.cs` на основе `HttpListener`: слушает порт из конфигурации, обслуживает статические файлы (`wwwroot`) и API.
2. В `AuthManager.cs` хранить токены в `ConcurrentDictionary`. Метод `Authenticate()` возвращает токен; метод `TryValidateToken()`. При смене порта вызывается `ClearAllSessions()`.
3. Страница авторизации (`index.html`) – форма логина, отправляет POST `/api/login`, получает токен и сохраняет его в cookie/Authorization header.
4. Неавторизованные запросы → 401. После успешного входа пользователь редиректится на `/home`.
5. Кнопка “Exit” вызывает `/api/logout` (удаление токена) и возвращает на страницу логина.

### Этап 3: SPA – вкладка **Home**
1. `HomeController.cs`: API `/api/status`, `/api/start`, `/api/stop`.
2. `LmsCommandExecutor.cs` оборачивает команды `lms server …`. Возвращает статус (`Running`, `Stopped`, `Unknown`, `Error`).
3. `StatusChecker.cs` – background‑task, каждые 60 секунд обновляет кэш статуса.
4. `js/home.js`: polling статуса (каждые 10 сек.), кнопки start/stop, отображение статуса с цветовой индикацией.

### Этап 4: SPA – вкладка **Settings**
1. `SettingsController.cs`: API `/api/settings` GET (возвращает username и port) / PUT (обновляет настройки). Пароль обновляется через хеш, при первом логине сохраняется в `config.json`.
2. При изменении порта: 1) обновить конфигурацию, 2) вызвать `AuthManager.ClearAllSessions()` → все сессии становятся недействительными, 3) перезапустить HTTP‑сервер (через `Program.RestartServices()`).
3. `js/settings.js`: форма с полями username, password, port; валидация поля порт (1–65535); отправка PUT.

### Этап 5: Публикация и тестирование
1. Настроить publish‑профиль:
   ```bash
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
``` 
2. Тестировать:
   * Консольный запуск → http://localhost:7778.
   * Запуск как служба (`-service`), проверка через `services.msc`.
   * Авторизация, работа вкладок, управление LM Studio.
   * Изменение порта – перезапуск сервера и инвалидирование сессий.
3. Проверить логирование в `logs/app.log`.

---

## 4. Формат HTTP‑ответов
- **GET /api/status** → `{status: "running", message: "LM Studio Server is running"}`
- **POST /api/start/stop** → `{success: true}` (или `500` при ошибке)
- **GET /api/settings** → `{username: "admin", port: 7778}`
- **PUT /api/settings** → `{success: true}` (ошибка → `400`)
- **POST /api/login** → `{token: "..."}` | `401`
- **POST /api/logout** → `{success: true}`

---

## 5. Критерии готовности
- Приложение запускается из консоли и открывает страницу на порту.
- Работает как служба Windows с параметром `-service`.
- Страница авторизации требует логин/пароль, после входа видны вкладки Home/Settings.
- Вкладка Home показывает статус LM Studio и позволяет старт/стоп.
- Вкладка Settings позволяет менять логин, пароль и порт; изменение порта перезапускает сервер и инвалидирует сессии.
- Настройки сохраняются в `config.json` (пароль хеширован).
- Статус LM Studio обновляется автоматически каждые 60 секунд.
- Кнопка Exit завершает сессию и возвращает на страницу входа.
