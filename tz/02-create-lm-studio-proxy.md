# Подробный план доработок: LM Studio Proxy & Port Display

**Дата:** 2026-08-26
**Статус:** На утверждении

---

## Цель
Расширить функциональность админ-панели: отображение порта и статуса LM Studio Server, проксирование всех API-запросов к LM Studio через админ-сервер для доступа с удалённых машин.

---

## Шаг 1. Добавить `LmStudioPort` и `BindAddress` в `AppConfig`

**Файл:** `Config/AppConfig.cs`

**Изменения:**
```csharp
public class AppConfig
{
    public string Username { get; set; } = "admin";
    public string Password { get; set; } = "admin";
    public int Port { get; set; } = 7778;           // порт админ-сервера
    public int LmStudioPort { get; set; } = 1234;   // порт LM Studio Server
    public string BindAddress { get; set; } = "0.0.0.0"; // привязка админ-сервера
}
```

**Обновить `config.json`:**
```json
{
    "username": "admin",
    "password": "admin",
    "port": 7778,
    "lmStudioPort": 1234,
    "bindAddress": "0.0.0.0"
}
```

---

## Шаг 2. Извлекать порт LM Studio из вывода `lms server status`

**Файл:** `Commands/LmsCommandExecutor.cs`

**Изменения:**

### 2.1. Добавить статическое поле для хранения порта
```csharp
private static int _cachedLmStudioPort = 1234; // дефолт
```

### 2.2. Обновить метод `GetStatus()` для парсинга порта
После выполнения `lms server status` извлечь порт из вывода. Примеры вывода `lms server status`:
```
LM Studio server is running
  Port: 1234
  Model: ...
```

**Регулярное выражение для парсинга:**
```csharp
// Ищем "port" + число в выводе
var portMatch = Regex.Match(output, @"port[\s:]+(\d+)", RegexOptions.IgnoreCase);
if (portMatch.Success && int.TryParse(portMatch.Groups[1].Value, out var port))
{
    _cachedLmStudioPort = port;
}
```

### 2.3. Добавить метод для получения порта
```csharp
public static int GetLmStudioPort()
{
    lock (_lock)
        return _cachedLmStudioPort;
}
```

### 2.4. Добавить метод для автоопределения порта
```csharp
public static bool TryAutoDetectLmStudioPort()
{
    // Выполнить lms server status и извлечь порт
    // Если порт не найден — вернуть false
    var status = GetStatus();
    if (status == LmsStatus.Running)
    {
        // Парсим порт из output
        // ...
        return true;
    }
    return false;
}
```

### 2.5. При старте — попытаться автоопределить порт
В `Program.cs.StartServices()`:
```csharp
// Попытка автоопределения порта LM Studio
LmsCommandExecutor.TryAutoDetectLmStudioPort();
```

---

## Шаг 3. Сохранять порт LM Studio в `config.json`

**Файл:** `Config/ConfigManager.cs` (без изменений, сериализация автоматическая)

**Файл:** `Program.cs`

**Изменения:** При загрузке конфига — синхронизировать `_cachedLmStudioPort` с `config.LmStudioPort`.

В `Program.Main()`:
```csharp
_config = ConfigManager.Load();
LmsCommandExecutor.SetLmStudioPort(_config.LmStudioPort);
```

При изменении порта через UI — сохранять в конфиг.

---

## Шаг 4. Привязка HTTP-сервера к `0.0.0.0`

**Файл:** `Server/HttpServer.cs`

**Изменения:**

### 4.1. Использовать `_config.BindAddress` вместо `localhost`
```csharp
public HttpServer(AppConfig config)
{
    _config = config;
    _listener = new HttpListener();
    _listener.Prefixes.Add($"http://{config.BindAddress}:{config.Port}/");
    Logger.Info($"HttpServer configured on {config.BindAddress}:{config.Port}");
}
```

### 4.2. При изменении BindAddress через UI — перезапустить сервер
(Аналогично поведению при смене порта)

---

## Шаг 5. Создать контроллер прокси LM Studio

**Новый файл:** `Server/LmStudioProxyController.cs`

### 5.1. Список проксируемых эндпоинтов
```csharp
private static readonly string[] ProxyPaths = new[]
{
    "/api/v1/models",
    "/api/v1/chat",
    "/api/v1/models/load",
    "/api/v1/models/download",
    "/v1/models",
    "/v1/responses",
    "/v1/chat/completions",
    "/v1/completions",
    "/v1/embeddings"
};
```

### 5.2. Метод `HandleProxyRequest`
```csharp
public static void HandleProxyRequest(HttpListenerContext context, AppConfig config)
{
    var request = context.Request;
    var path = request.Url?.AbsolutePath ?? "/";
    var method = request.HttpMethod;

    // Проверяем, является ли путь проксируемым
    var isProxyPath = ProxyPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase))
                      || path.StartsWith("/api/v1/models/download/status", StringComparison.OrdinalIgnoreCase);

    if (!isProxyPath)
    {
        SendNotFound(context);
        return;
    }

    // Получаем порт LM Studio
    var lmStudioPort = config.LmStudioPort;
    var targetUrl = $"http://localhost:{lmStudioPort}{path}";

    // Создаём прокси-запрос
    try
    {
        var proxyRequest = (HttpWebRequest)WebRequest.Create(targetUrl);
        proxyRequest.Method = method;
        proxyRequest.ContentType = request.ContentType ?? "application/json";

        // Копируем заголовки (исключая некоторые)
        foreach (var header in request.Headers)
        {
            var key = header.Key as string;
            if (key != null && !IgnoredHeaders.Contains(key.ToLower()))
            {
                // Устанавливаем заголовок (обработка дубликатов)
                try { proxyRequest.Headers.Add(key, header.Value as string); } catch { }
            }
        }

        // Копируем тело запроса
        if (request.HasInputStream && method != "GET" && method != "HEAD")
        {
            using var inputStream = request.InputStream;
            using var ms = new MemoryStream();
            inputStream.CopyTo(ms);
            proxyRequest.ContentLength64 = ms.Length;
            if (ms.Length > 0)
            {
                using var requestStream = proxyRequest.GetRequestStream();
                ms.Position = 0;
                ms.CopyTo(requestStream);
            }
        }

        // Получаем ответ
        using var response = (HttpWebResponse)proxyRequest.GetResponse();
        var responseStream = response.GetResponseStream();

        context.Response.StatusCode = response.StatusCode;
        context.Response.ContentType = response.ContentType ?? "application/json";

        if (responseStream != null)
        {
            responseStream.CopyTo(context.Response.OutputStream);
        }

        context.Response.OutputStream.Close();
    }
    catch (WebException ex)
    {
        // LM Studio недоступен
        var errorStatus = ((ex.Response as HttpWebResponse)?.StatusCode ?? HttpStatusCode.BadGateway);
        SendJsonResponse(context, errorStatus,
            JsonSerializer.Serialize(new { error = $"LM Studio Server unavailable (port {lmStudioPort})" }));
    }
    catch (Exception ex)
    {
        Logger.Error($"Proxy error: {ex.Message}", ex);
        SendJsonResponse(context, HttpStatusCode.BadGateway,
            JsonSerializer.Serialize(new { error = "Proxy error" }));
    }
}
```

### 5.3. Добавить обработку прокси в `HttpServer.cs`
В методе `HandleRequest()`, после проверки авторизации (но до статических файлов):

```csharp
// LM Studio Proxy
if (path.StartsWith("/api/v1/") || path.StartsWith("/v1/"))
{
    LmStudioProxyController.HandleProxyRequest(context, _config);
    return;
}
```

---

## Шаг 6. Добавить эндпоинт для получения информации о LM Studio

**Файл:** `Server/HomeController.cs`

**Добавить новый эндпоинт `GET /api/lmstudio/info`:**

```csharp
public static void GetLmStudioInfo(HttpListenerContext context, AppConfig config)
{
    var lmStudioPort = LmsCommandExecutor.GetLmStudioPort();
    var currentStatus = LmsCommandExecutor.GetStatus();
    
    // Получаем модель — пробуем через API
    string currentModel = "N/A";
    try
    {
        var modelsRequest = (HttpWebRequest)WebRequest.Create($"http://localhost:{lmStudioPort}/v1/models");
        using var modelsResponse = (HttpWebResponse)modelsRequest.GetResponse();
        using var reader = new StreamReader(modelsResponse.GetResponseStream());
        var modelsJson = reader.ReadToEnd();
        var models = JsonSerializer.Deserialize<ModelsResponse>(modelsJson);
        if (models?.Data?.Count > 0)
        {
            currentModel = models.Data[0].Id;
        }
    }
    catch
    {
        currentModel = "No model loaded";
    }

    var response = JsonSerializer.Serialize(new
    {
        port = lmStudioPort,
        status = currentStatus.ToString().ToLower(),
        model = currentModel,
        connected = currentStatus == LmsStatus.Running
    });

    SendJsonResponse(context, HttpStatusCode.OK, response);
}
```

**Вспомогательные классы:**
```csharp
private class ModelsResponse
{
    public List<ModelInfo> Data { get; set; } = new();
}

private class ModelInfo
{
    public string Id { get; set; } = "";
}
```

---

## Шаг 7. Обновить `HomeController` для передачи `AppConfig`

**Файл:** `Server/HomeController.cs`

**Изменения:** Добавить `AppConfig` как параметр в методы, чтобы иметь доступ к порту LM Studio.

**Файл:** `Server/HttpServer.cs`

**Изменения:** Обновить вызовы контроллеров:
```csharp
// Home API
if (path == "/api/status" && method == "GET")
{
    HomeController.GetStatus(context);
    return;
}

// LM Studio Info
if (path == "/api/lmstudio/info" && method == "GET")
{
    HomeController.GetLmStudioInfo(context, _config);
    return;
}
```

---

## Шаг 8. Обновить UI — вкладка Home

**Файл:** `wwwroot/index.html`

**Изменения в секции Home Tab:**
```html
<div id="home-tab" class="tab-content">
    <div class="card">
        <h2>LM Studio Server Status</h2>
        <div class="status-display">
            <span id="status-badge" class="badge badge-unknown">Unknown</span>
            <span id="status-message">Loading...</span>
        </div>
        <div class="actions">
            <button id="start-btn" class="btn btn-success">Start</button>
            <button id="stop-btn" class="btn btn-danger">Stop</button>
        </div>
    </div>

    <!-- NEW: LM Studio Connection Info -->
    <div class="card">
        <h2>LM Studio Connection</h2>
        <div class="connection-info">
            <div class="info-row">
                <span class="info-label">Port:</span>
                <span id="lmstudio-port">—</span>
            </div>
            <div class="info-row">
                <span class="info-label">Model:</span>
                <span id="lmstudio-model">—</span>
            </div>
            <div class="info-row">
                <span class="info-label">Status:</span>
                <span id="lmstudio-connection-status">—</span>
            </div>
        </div>
    </div>
</div>
```

**Добавить CSS:**
```css
.connection-info {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}

.info-row {
    display: flex;
    justify-content: space-between;
    padding: 0.5rem 0;
    border-bottom: 1px solid var(--border);
}

.info-row:last-child {
    border-bottom: none;
}

.info-label {
    font-weight: 600;
    color: var(--text-muted);
}
```

---

## Шаг 9. Обновить UI — вкладка Settings

**Файл:** `wwwroot/index.html`

**Добавить в Settings:**
```html
<div class="form-group">
    <label for="settings-lmstudio-port">LM Studio Server Port</label>
    <div class="input-with-button">
        <input type="number" id="settings-lmstudio-port" name="lmStudioPort" min="1" max="65535" required>
        <button type="button" id="auto-detect-port-btn" class="btn btn-secondary btn-sm">Auto-detect</button>
    </div>
</div>
```

**Файл:** `wwwroot/js/settings.js`

**Добавить логику:**
```javascript
// Загрузка порта LM Studio
async loadLmStudioPort() {
    try {
        const data = await App.apiFetch('/api/settings/lmstudio');
        if (data) {
            document.getElementById('settings-lmstudio-port').value = data.lmStudioPort || 1234;
        }
    } catch (e) {
        // Ignore
    }
}

// Автоопределение порта
async autoDetectPort() {
    const btn = document.getElementById('auto-detect-port-btn');
    btn.disabled = true;
    btn.textContent = 'Detecting...';
    
    try {
        const data = await App.apiFetch('/api/settings/lmstudio/detect', { method: 'POST' });
        if (data && data.success) {
            document.getElementById('settings-lmstudio-port').value = data.port;
            Toast.show(`Port detected: ${data.port}`, 'success');
        }
    } catch (e) {
        Toast.show('Failed to detect port', 'error');
    } finally {
        btn.disabled = false;
        btn.textContent = 'Auto-detect';
    }
}
```

**Файл:** `wwwroot/index.html` — добавить кнопку в форму settings

---

## Шаг 10. Добавить эндпоинт для управления настройками LM Studio

**Файл:** `Server/SettingsController.cs`

**Новые эндпоинты:**

### GET `/api/settings/lmstudio`
```csharp
public static void GetLmStudioSettings(HttpListenerContext context, AppConfig config)
{
    var response = JsonSerializer.Serialize(new
    {
        lmStudioPort = config.LmStudioPort,
        bindAddress = config.BindAddress
    }, _jsonOptions);
    SendJsonResponse(context, HttpStatusCode.OK, response);
}
```

### POST `/api/settings/lmstudio/detect`
```csharp
public static void AutoDetectLmStudioPort(HttpListenerContext context)
{
    var success = LmsCommandExecutor.TryAutoDetectLmStudioPort();
    var port = LmsCommandExecutor.GetLmStudioPort();
    
    var response = JsonSerializer.Serialize(new
    {
        success = success,
        port = port
    }, _jsonOptions);
    
    SendJsonResponse(context, success ? HttpStatusCode.OK : HttpStatusCode.NotFound, response);
}
```

### PUT `/api/settings/lmstudio`
```csharp
public static void UpdateLmStudioSettings(HttpListenerContext context, AppConfig config)
{
    // Обновление порта LM Studio и bind address
    // Сохранение в config
}
```

---

## Шаг 11. Обновить `Program.cs`

**Файл:** `Program.cs`

**Изменения:**
```csharp
private static void StartServices()
{
    // Запуск HTTP сервера
    _httpServer = new HttpServer(_config!);
    _httpServer.Start();

    // Запуск проверки статуса
    _statusChecker = new StatusChecker(status =>
    {
        Logger.Info($"Status changed: {status}");
    });

    // Первоначальная проверка статуса и автоопределение порта
    LmsCommandExecutor.GetStatus();
    LmsCommandExecutor.TryAutoDetectLmStudioPort();
}
```

---

## Шаг 12. Обновить документацию

**Файл:** `docs/areas/backend-api-structure.md`

**Добавить новые эндпоинты:**
```
GET    /api/lmstudio/info         – Информация о LM Studio Server (порт, модель, статус)
GET    /api/settings/lmstudio     – Настройки LM Studio (порт, bind address)
POST   /api/settings/lmstudio/detect – Автоопределение порта LM Studio
PUT    /api/settings/lmstudio     – Обновление настроек LM Studio
```

**Файл:** `docs/project-overview.md`

**Обновить описание компонентов** — добавить `LmStudioProxyController`.

---

## Итоговый список файлов для изменения

| Файл | Действие |
|------|----------|
| `Config/AppConfig.cs` | Изменить: добавить `LmStudioPort`, `BindAddress` |
| `Config/ConfigManager.cs` | Без изменений (автосериализация) |
| `Commands/LmsCommandExecutor.cs` | Изменить: парсинг порта, методы `GetLmStudioPort()`, `TryAutoDetectLmStudioPort()` |
| `Server/HttpServer.cs` | Изменить: привязка к `BindAddress`, маршрутизация прокси |
| `Server/HomeController.cs` | Изменить: добавить `GetLmStudioInfo()` |
| `Server/SettingsController.cs` | Изменить: добавить эндпоинты для LM Studio настроек |
| `Server/LmStudioProxyController.cs` | **Новый файл**: прокси-контроллер |
| `Program.cs` | Изменить: автоопределение порта при старте |
| `wwwroot/index.html` | Изменить: добавить карточку LM Studio Connection, поле LM Studio Port в Settings |
| `wwwroot/js/home.js` | Изменить: отображение порта, модели, статуса подключения |
| `wwwroot/js/settings.js` | Изменить: загрузка/сохранение LM Studio настроек, автоопределение |
| `wwwroot/css/style.css` | Изменить: стили для connection info |
| `docs/areas/backend-api-structure.md` | Изменить: добавить новые эндпоинты |
| `docs/project-overview.md` | Изменить: обновить описание |
| `config.json` | Изменить: добавить `lmStudioPort`, `bindAddress` |

---

## Порядок выполнения

1. ✅ Анализ документации и кода
2. ✅ Формулировка вопросов и получение ответов
3. ✅ Создание плана
4. [ ] Шаг 1: Обновить `AppConfig`
5. [ ] Шаг 2: Парсинг порта из `lms server status`
6. [ ] Шаг 3: Синхронизация порта с `config.json`
7. [ ] Шаг 4: Привязка к `0.0.0.0`
8. [ ] Шаг 5: Создать `LmStudioProxyController.cs`
9. [ ] Шаг 6: Добавить эндпоинт `/api/lmstudio/info`
10. [ ] Шаг 7: Обновить маршрутизацию в `HttpServer.cs`
11. [ ] Шаг 8: Обновить UI — вкладка Home
12. [ ] Шаг 9: Обновить UI — вкладка Settings
13. [ ] Шаг 10: Добавить эндпоинты настроек LM Studio
14. [ ] Шаг 11: Обновить `Program.cs`
15. [ ] Шаг 12: Обновить документацию
16. [ ] Проверка сборки и тестирование

---

## Риски и замечания

1. **Парсинг вывода `lms server status`:** Формат вывода может меняться между версиями LM Studio. Рекомендуется тестировать на конкретной версии.
2. **Безопасность прокси:** Прокси открыт только для аутентифицированных пользователей. Не рекомендуется разворачивать на публичных интерфейсах без дополнительной защиты.
3. **Timeout-ы:** Прокси-запросы к LM Studio могут быть долгими (chat completions). Нужно настроить таймауты `HttpWebRequest`.
4. **Streaming:** Для эндпоинтов с streaming (chat/completions с `stream: true`) нужно реализовать потоковую передачу ответа.
5. **Большие тела запросов:** При проксировании загрузки моделей нужно учитывать лимиты `maxRequestLength`.
