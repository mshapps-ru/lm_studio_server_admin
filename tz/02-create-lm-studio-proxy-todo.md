# To-Do List: LM Studio Proxy & Port Display

**Создан:** 2026-08-26
**Статус:** Ожидает начала реализации

---

## Backend

### 1. [ ] Обновить `Config/AppConfig.cs`
- [ ] Добавить свойство `LmStudioPort` (int, default 1234)
- [ ] Добавить свойство `BindAddress` (string, default "0.0.0.0")
- [ ] Обновить `config.json` с новыми полями

### 2. [ ] Обновить `Commands/LmsCommandExecutor.cs`
- [ ] Добавить поле `_cachedLmStudioPort`
- [ ] Добавить метод `GetLmStudioPort()`
- [ ] Добавить метод `SetLmStudioPort(int port)`
- [ ] Добавить метод `TryAutoDetectLmStudioPort()`
- [ ] Обновить `GetStatus()` для парсинга порта из вывода `lms server status`
- [ ] Добавить regex для извлечения порта: `port[\s:]+(\d+)`

### 3. [ ] Создать `Server/LmStudioProxyController.cs` (НОВЫЙ ФАЙЛ)
- [ ] Определить список проксируемых путей
- [ ] Реализовать `HandleProxyRequest(HttpListenerContext, AppConfig)`
- [ ] Копировать метод, путь, заголовки, тело запроса
- [ ] Обработка ошибок (WebException → BadGateway)
- [ ] Добавить список игнорируемых заголовков

### 4. [ ] Обновить `Server/HomeController.cs`
- [ ] Добавить метод `GetLmStudioInfo(HttpListenerContext, AppConfig)`
- [ ] Добавить вспомогательные классы `ModelsResponse`, `ModelInfo`
- [ ] Запрос к `/v1/models` для определения текущей модели
- [ ] Возврат: port, status, model, connected

### 5. [ ] Обновить `Server/SettingsController.cs`
- [ ] Добавить `GetLmStudioSettings(HttpListenerContext, AppConfig)` — GET `/api/settings/lmstudio`
- [ ] Добавить `AutoDetectLmStudioPort(HttpListenerContext)` — POST `/api/settings/lmstudio/detect`
- [ ] Добавить `UpdateLmStudioSettings(HttpListenerContext, AppConfig)` — PUT `/api/settings/lmstudio`

### 6. [ ] Обновить `Server/HttpServer.cs`
- [ ] Использовать `_config.BindAddress` вместо `localhost` в `_listener.Prefixes`
- [ ] Добавить маршрутизацию для `/api/lmstudio/info` → `HomeController.GetLmStudioInfo`
- [ ] Добавить маршрутизацию для `/api/settings/lmstudio*` → `SettingsController`
- [ ] Добавить маршрутизацию для `/api/v1/*` и `/v1/*` → `LmStudioProxyController.HandleProxyRequest`

### 7. [ ] Обновить `Program.cs`
- [ ] В `StartServices()`: вызвать `LmsCommandExecutor.TryAutoDetectLmStudioPort()`
- [ ] Синхронизировать `_cachedLmStudioPort` с `config.LmStudioPort` при загрузке

---

## Frontend

### 8. [ ] Обновить `wwwroot/index.html`
- [ ] Добавить карточку "LM Studio Connection" во вкладку Home
- [ ] Добавить элементы: `#lmstudio-port`, `#lmstudio-model`, `#lmstudio-connection-status`
- [ ] Добавить поле "LM Studio Server Port" + кнопку "Auto-detect" во вкладку Settings
- [ ] Добавить `id="settings-lmstudio-port"` для input

### 9. [ ] Обновить `wwwroot/js/home.js`
- [ ] Добавить метод `getLmStudioInfo()` — запрос к `/api/lmstudio/info`
- [ ] Добавить метод `updateConnectionInfo(data)` — обновление UI
- [ ] Вызывать `getLmStudioInfo()` при `init()` и в polling-цикле
- [ ] Обновить `updateStatus()` для отображения connected/disconnected

### 10. [ ] Обновить `wwwroot/js/settings.js`
- [ ] Добавить метод `loadLmStudioPort()` — загрузка порта из `/api/settings/lmstudio`
- [ ] Добавить метод `autoDetectPort()` — POST к `/api/settings/lmstudio/detect`
- [ ] Привязать событие клика на кнопку "Auto-detect"
- [ ] В `saveSettings()`: включать `lmStudioPort` в payload
- [ ] Добавить эндпоинт PUT `/api/settings/lmstudio` для сохранения

### 11. [ ] Обновить `wwwroot/css/style.css`
- [ ] Добавить стили для `.connection-info`
- [ ] Добавить стили для `.info-row`, `.info-label`
- [ ] Добавить стили для `.input-with-button` (flex-контейнер для input + button)

---

## Documentation

### 12. [ ] Обновить `docs/areas/backend-api-structure.md`
- [ ] Добавить эндпоинт `GET /api/lmstudio/info`
- [ ] Добавить эндпоинт `GET /api/settings/lmstudio`
- [ ] Добавить эндпоинт `POST /api/settings/lmstudio/detect`
- [ ] Добавить эндпоинт `PUT /api/settings/lmstudio`
- [ ] Добавить описание формата ответов

### 13. [ ] Обновить `docs/project-overview.md`
- [ ] Добавить `LmStudioProxyController` в описание компонентов
- [ ] Обновить секцию "Typical Usage"

---

## Testing

### 14. [ ] Проверка сборки
- [ ] `dotnet build` — без ошибок
- [ ] `dotnet run` — сервер стартует на 0.0.0.0

### 15. [ ] Функциональное тестирование
- [ ] Порт LM Studio отображается на Home
- [ ] Модель отображается на Home
- [ ] Статус подключения (connected/disconnected) отображается
- [ ] Кнопка "Auto-detect" определяет порт
- [ ] Порт сохраняется в config.json
- [ ] Прокси работает для `/v1/models`
- [ ] Прокси работает для `/v1/chat/completions`
- [ ] Прокси работает для `/api/v1/chat`
- [ ] Доступ с удалённой машины (через IP:порт)
- [ ] Настройки LM Studio сохраняются

---

## Риски

- [ ] Формат вывода `lms server status` может отличаться — проверить на целевой системе
- [ ] Streaming-запросы требуют отдельной обработки
- [ ] Большие тела запросов (загрузка моделей) — проверить лимиты
- [ ] Таймауты для долгих запросов (chat completions)
