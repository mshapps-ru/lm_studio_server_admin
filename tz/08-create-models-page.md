# План создания страницы управления моделями

## 1. Обзор

Цель: создать новую вкладку **Models** в существующей навигации (Home, Settings, Models), на которой будет отображаться:
- Список моделей из `config.json` → `LmStudioModelList`
- Параметры запуска для каждой модели из `config.json` → `LmStudioModelLoadParameterList`
- Дефолтные параметры запуска из `config.json` → `LmStudioModelDefaultLoadParameter`
- Кнопки управления: Load/Unload для каждой модели
- Возможность редактирования и сохранения параметров модели и дефолтных параметров

### 1.1 Текущее состояние
- **Навигация**: вкладки Home и Settings в `index.html`
- **JS-логика вкладок**: `App.setupTabs()` в `wwwroot/js/app.js`
- **Структура config.json**:
  - `LmStudioModelList` — `List<ModelInfo>` (Id, Object, Owned_by)
  - `LmStudioModelLoadParameterList` — `List<ModelLoadParametersEntry>` (Model, Parameters: `Dictionary<string, int?>`)
  - `LmStudioModelDefaultLoadParameter` — `Dictionary<string, int?>`
- **Параметры моделей**: текущий тип `Dictionary<string, int?>` — только целочисленные значения
- **API паттерн**: контроллеры в `Server/`, endpoints регистрируются в `HttpServer.HandleRequest()`
- **Frontend паттерн**: отдельные JS модули (`home.js`, `settings.js`), общий `app.js` для навигации и API

### 1.2 Целевое состояние
| Компонент | Описание |
|-----------|----------|
| **Новая вкладка "Models"** | Добавлена в навигацию (Home, Settings, Models) |
| **Список моделей** | Отображается в виде карточек/таблицы с информацией о каждой модели |
| **Параметры модели** | Для каждой модели отображаются и редактируются параметры из `LmStudioModelLoadParameterList` |
| **Дефолтные параметры** | Редактируемая секция с дефолтными параметрами из `LmStudioModelDefaultLoadParameter` |
| **Load/Unload кнопки** | Для каждой модели кнопки загрузки и выгрузки |
| **Кнопка обновления** | Ручное обновление списка моделей |
| **Расширенные типы параметров** | Поддержка int, double, bool, string вместо только int |

## 2. Ответы на вопросы

1. **Формат страницы**: Новая вкладка (tab) в существующей навигации (Home, Settings, Models)
2. **Load/Unload действия**: Да, добавить кнопки Load/Unload для каждой модели
3. **Типы параметров**: Расширить до string, bool, double, int
4. **Ручное обновление списка**: Добавить кнопку "Обновить список" для ручного обновления

## 3. Решения по архитектуре

### 3.1 Изменения в модели данных (AppConfig.cs)

**Текущий тип:**
```csharp
public Dictionary<string, int?>? Parameters { get; set; }
```

**Новый тип:**
```csharp
public Dictionary<string, ModelParameter>? Parameters { get; set; }

public class ModelParameter
{
    public string? Value { get; set; }  // unified string representation
    public ParameterType Type { get; set; }  // Int, Double, Bool, String
}

public enum ParameterType
{
    Int,
    Double,
    Bool,
    String
}
```

**Обоснование**: unified string storage с явным типом позволяет сериализовать все типы в JSON и корректно десериализовать обратно. UI будет отображать разные инпуты в зависимости от типа.

### 3.2 Новый контроллер (ModelController.cs)

Новый статический контроллер в `Server/ModelController.cs` с endpoints:

| Method | Endpoint | Описание |
|--------|----------|----------|
| GET | `/api/models` | Получить список моделей из config |
| GET | `/api/models/parameters/default` | Получить дефолтные параметры |
| PUT | `/api/models/parameters/default` | Сохранить дефолтные параметры |
| GET | `/api/models/parameters/{modelName}` | Получить параметры конкретной модели |
| PUT | `/api/models/parameters/{modelName}` | Сохранить параметры конкретной модели |
| POST | `/api/models/{modelName}/load` | Загрузить модель |
| POST | `/api/models/{modelName}/unload` | Выгрузить модель |
| POST | `/api/models/refresh` | Обновить список моделей из LM Studio |

### 3.3 Frontend структура

- `wwwroot/js/models.js` — основная логика страницы моделей
- HTML-разметка для вкладки Models в `index.html`
- CSS-стили в `wwwroot/css/style.css` (дополнительные стили для карточек моделей)

### 3.4 Паттерны проектирования

- Следовать существующему паттерну: статический контроллер + HttpListener context
- Использовать существующий `App.apiFetch()` для API вызовов
- Использовать существующую систему табов (`App.setupTabs()`)
- Использовать существующую систему уведомлений (`Toast.show()`)
- Использовать существующие CSS-переменные и классы

## 4. Шаги реализации

### Шаг 1: Обновить модели данных (Config/AppConfig.cs)

1.1. Добавить класс `ModelParameter` с полями `Value` (string) и `Type` (enum)
1.2. Добавить enum `ParameterType` с значениями Int, Double, Bool, String
1.3. Изменить тип `Parameters` в `ModelLoadParametersEntry` на `Dictionary<string, ModelParameter>?`
1.4. Изменить тип `LmStudioModelDefaultLoadParameter` на `Dictionary<string, ModelParameter>?`
1.5. Добавить helper-методы для конвертации между старым и новым форматами (для обратной совместимости)
1.6. Добавить helper-методы для парсинга строковых значений в соответствующие типы

### Шаг 2: Создать контроллер ModelController.cs

2.1. Создать файл `Server/ModelController.cs`
2.2. Реализовать `GetModels()` — GET `/api/models`
   - Возвращает `LmStudioModelList` из config
   - Format: `{ "models": [ { "id": "...", "object": "...", "owned_by": "..." } ] }`
2.3. Реализовать `GetDefaultParameters()` — GET `/api/models/parameters/default`
   - Возвращает `LmStudioModelDefaultLoadParameter` из config
2.4. Реализовать `UpdateDefaultParameters()` — PUT `/api/models/parameters/default`
   - Принимает JSON с параметрами и сохраняет в config
2.5. Реализовать `GetModelParameters()` — GET `/api/models/parameters/{modelName}`
   - Ищет entry в `LmStudioModelLoadParameterList` по имени модели
   - Возвращает параметры или empty dict если нет entry
2.6. Реализовать `UpdateModelParameters()` — PUT `/api/models/parameters/{modelName}`
   - Обновляет или создаёт entry в `LmStudioModelLoadParameterList`
2.7. Реализовать `LoadModel()` — POST `/api/models/{modelName}/load`
   - Вызывает `LmsCommandExecutor.LoadModel(modelName)`
2.8. Реализовать `UnloadModel()` — POST `/api/models/{modelName}/unload`
   - Вызывает `LmsCommandExecutor.UnloadModel(modelName)` (нужно добавить публичный метод)
2.9. Реализовать `RefreshModels()` — POST `/api/models/refresh`
   - Делает запрос к LM Studio `/v1/models` и обновляет `LmStudioModelList` в config

### Шаг 3: Обновить HttpServer.cs

3.1. Добавить маршрутизацию для новых endpoints в `HandleRequest()`
3.2. Добавить маршруты:
   - `/api/models` → GET → `ModelController.GetModels()`
   - `/api/models/parameters/default` → GET/PUT → `ModelController.GetDefaultParameters()` / `UpdateDefaultParameters()`
   - `/api/models/parameters/{name}` → GET/PUT → `ModelController.GetModelParameters()` / `UpdateModelParameters()`
   - `/api/models/{name}/load` → POST → `ModelController.LoadModel()`
   - `/api/models/{name}/unload` → POST → `ModelController.UnloadModel()`
   - `/api/models/refresh` → POST → `ModelController.RefreshModels()`

### Шаг 4: Обновить LmsCommandExecutor.cs

4.1. Добавить публичный метод `UnloadModelPublic(string modelName)` для выгрузки конкретной модели
   - Обёртка над существующим приватным `UnloadModel()`
   - Или сделать `UnloadModel` публичным и переименовать внутренний

### Шаг 5: Обновить HTML (wwwroot/index.html)

5.1. Добавить кнопку вкладки в `<nav class="tabs">`:
   ```html
   <button class="tab" data-tab="models">Models</button>
   ```
5.2. Добавить контейнер для вкладки Models в `<main class="content">`:
   ```html
   <div id="models-tab" class="tab-content">
       <!-- Секция списка моделей -->
       <div class="card">
           <h2>Available Models</h2>
           <button id="refresh-models-btn" class="btn btn-secondary btn-sm">Refresh</button>
           <div id="models-list"></div>
       </div>
       <!-- Секция дефолтных параметров -->
       <div class="card">
           <h2>Default Load Parameters</h2>
           <div id="default-parameters-editor"></div>
           <button id="save-default-params-btn" class="btn btn-primary">Save Default Parameters</button>
       </div>
   </div>
   ```
5.3. Добавить подключение скрипта в `<body>`:
   ```html
   <script src="/js/models.js"></script>
   ```

### Шаг 6: Создать JS модуль (wwwroot/js/models.js)

6.1. Создать объект `Models` с методами:
   - `init()` — инициализация, привязка событий, загрузка данных
   - `loadModels()` — загрузка списка моделей из `/api/models`
   - `renderModelsList(models)` — рендеринг списка моделей в карточки
   - `loadModelParameters(modelName)` — загрузка параметров модели
   - `renderModelParameters(modelName, parameters)` — рендеринг редактора параметров модели
   - `loadDefaultParameters()` — загрузка дефолтных параметров
   - `renderDefaultParameters(parameters)` — рендеринг редактора дефолтных параметров
   - `saveModelParameters(modelName)` — сохранение параметров модели
   - `saveDefaultParameters()` — сохранение дефолтных параметров
   - `loadModel(modelName)` — вызов Load API
   - `unloadModel(modelName)` — вызов Unload API
   - `refreshModels()` — обновление списка моделей

6.2. Структура карточки модели:
```html
<div class="model-card" data-model-id="model-id">
    <div class="model-header">
        <h3>model-name</h3>
        <span class="model-owned-by">owned_by</span>
    </div>
    <div class="model-actions">
        <button class="btn btn-success btn-sm btn-load" data-model="name">Load</button>
        <button class="btn btn-danger btn-sm btn-unload" data-model="name">Unload</button>
        <button class="btn btn-secondary btn-sm btn-edit-params" data-model="name">Edit Parameters</button>
    </div>
    <div class="model-params-editor" style="display:none">
        <!-- Редактор параметров -->
        <div class="param-row">
            <label>param-name</label>
            <select class="param-type">...</select>
            <input type="text" class="param-value" />
        </div>
        <button class="btn btn-primary btn-sm btn-save-params">Save</button>
        <button class="btn btn-secondary btn-sm btn-cancel-params">Cancel</button>
    </div>
</div>
```

6.3. Структура редактора параметров:
- Каждая пара ключ-значение отображается как строка с:
  - Полем имени параметра (input text)
  - Селектором типа (int, double, bool, string)
  - Полем значения (зависит от типа: number, checkbox, text)
- Кнопка "Добавить параметр" для добавления новых
- Кнопка "Удалить" для удаления существующих
- Кнопка "Сохранить" для сохранения в API
- Кнопка "Отмена" для закрытия редактора

6.4. Интеграция с `App.showApp()`:
- Добавить вызов `Models.init()` в метод `showApp()` в `app.js`

### Шаг 7: Обновить CSS (wwwroot/css/style.css)

7.1. Добавить стили для карточек моделей:
```css
.model-card {
    background: var(--card-bg);
    border: 1px solid var(--border);
    border-radius: var(--radius);
    padding: 1rem;
    margin-bottom: 1rem;
}

.model-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 0.75rem;
}

.model-header h3 {
    margin: 0;
    font-size: 1rem;
    color: var(--text);
}

.model-owned-by {
    font-size: 0.8rem;
    color: var(--text-muted);
}

.model-actions {
    display: flex;
    gap: 0.5rem;
    margin-bottom: 0.75rem;
}

.param-row {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    margin-bottom: 0.5rem;
}

.param-row label {
    flex: 2;
    font-weight: 500;
    font-size: 0.85rem;
}

.param-row input, .param-row select {
    flex: 1;
    padding: 0.4rem 0.6rem;
    border: 1px solid var(--border);
    border-radius: 4px;
    font-size: 0.85rem;
}

.btn-remove-param {
    background: var(--danger);
    color: white;
    border: none;
    border-radius: 4px;
    padding: 0.4rem 0.6rem;
    cursor: pointer;
}
```

7.2. Добавить стили для состояния загрузки:
```css
.loading-spinner {
    display: inline-block;
    width: 20px;
    height: 20px;
    border: 2px solid var(--border);
    border-top-color: var(--primary);
    border-radius: 50%;
    animation: spin 0.8s linear infinite;
}

@keyframes spin {
    to { transform: rotate(360deg); }
}
```

### Шаг 8: Обновить app.js

8.1. Добавить вызов `Models.init()` в метод `showApp()`:
```javascript
showApp() {
    // ... existing code ...
    Home.init();
    Settings.init();
    Models.init();  // <-- добавить
}
```

### Шаг 9: Обратная совместимость и миграция

9.1. В `ConfigManager.Load()` добавить логику миграции:
- Если `Parameters` содержит старые значения (int?), конвертировать в новый формат `ModelParameter`
- Если `LmStudioModelDefaultLoadParameter` содержит старые значения, конвертировать
9.2. Добавить helper-метод в `AppConfig.cs`:
```csharp
public static void MigrateConfig(AppConfig config)
{
    // Конвертация LmStudioModelLoadParameterList
    if (config.LmStudioModelLoadParameterList != null)
    {
        foreach (var entry in config.LmStudioModelLoadParameterList)
        {
            if (entry.Parameters != null)
            {
                entry.Parameters = MigrateParameters(entry.Parameters);
            }
        }
    }
    // Конвертация LmStudioModelDefaultLoadParameter
    if (config.LmStudioModelDefaultLoadParameter != null)
    {
        config.LmStudioModelDefaultLoadParameter = MigrateParameters(config.LmStudioModelDefaultLoadParameter);
    }
}

private static Dictionary<string, ModelParameter>? MigrateParameters(Dictionary<string, int?> oldParams)
{
    var newParams = new Dictionary<string, ModelParameter>();
    foreach (var kv in oldParams)
    {
        newParams[kv.Key] = new ModelParameter
        {
            Value = kv.Value.HasValue ? kv.Value.Value.ToString() : null,
            Type = ParameterType.Int
        };
    }
    return newParams;
}
```

### Шаг 10: Тестирование

10.1. Проверить загрузку списка моделей
10.2. Проверить отображение параметров для каждой модели
10.3. Проверить редактирование и сохранение параметров модели
10.4. Проверить редактирование и сохранение дефолтных параметров
10.5. Проверить Load/Unload кнопки
10.6. Проверить кнопку обновления списка моделей
10.7. Проверить обратную совместимость с существующим config.json
10.8. Проверить работу с разными типами параметров (int, double, bool, string)
10.9. Проверить обработку ошибок (нет подключения к LM Studio, модель не найдена и т.д.)

## 5. Файлы для изменения

| Файл | Тип | Описание |
|------|-----|----------|
| `Config/AppConfig.cs` | Изменить | Добавить ModelParameter, ParameterType, миграцию |
| `Server/ModelController.cs` | Новый | Новый контроллер для моделей |
| `Server/HttpServer.cs` | Изменить | Добавить маршруты для новых endpoints |
| `Commands/LmsCommandExecutor.cs` | Изменить | Добавить публичный метод UnloadModel |
| `wwwroot/index.html` | Изменить | Добавить вкладку Models и разметку |
| `wwwroot/js/app.js` | Изменить | Добавить Models.init() в showApp() |
| `wwwroot/js/models.js` | Новый | JS модуль для страницы моделей |
| `wwwroot/css/style.css` | Изменить | Добавить стили для карточек моделей |
| `config.json` | Миграция | Автоматическая при первом запуске |
| `docs/areas/backend-api-structure.md` | Обновить | Добавить документацию по новым endpoints |

## 6. Примеры API ответов

### GET /api/models
```json
{
  "models": [
    {
      "id": "gpt-oss-20b",
      "object": "model",
      "owned_by": "microsoft"
    },
    {
      "id": "qwen/qwen3.8-27b",
      "object": "model",
      "owned_by": "alibaba"
    }
  ]
}
```

### GET /api/models/parameters/default
```json
{
  "contextWindow": { "value": "10000", "type": "Int" },
  "ttl": { "value": "300", "type": "Int" }
}
```

### PUT /api/models/parameters/default
Request body:
```json
{
  "contextWindow": { "value": "10000", "type": "Int" },
  "ttl": { "value": "300", "type": "Int" },
  "gpuLayers": { "value": "50", "type": "Int" }
}
```
Response:
```json
{ "success": true }
```

### GET /api/models/parameters/{modelName}
```json
{
  "contextWindow": { "value": "100000", "type": "Int" },
  "ttl": { "value": "1200", "type": "Int" }
}
```

### PUT /api/models/parameters/{modelName}
Request body:
```json
{
  "contextWindow": { "value": "50000", "type": "Int" },
  "ttl": { "value": "600", "type": "Int" }
}
```
Response:
```json
{ "success": true }
```

### POST /api/models/{modelName}/load
Response:
```json
{ "success": true, "message": "Model loaded successfully" }
```

### POST /api/models/{modelName}/unload
Response:
```json
{ "success": true, "message": "Model unloaded successfully" }
```

### POST /api/models/refresh
Response:
```json
{ "success": true, "modelCount": 15 }
```

## 7. Порядок выполнения

1. Обновить модели данных (AppConfig.cs) — Шаг 1
2. Добавить публичный метод UnloadModel в LmsCommandExecutor — Шаг 4
3. Создать ModelController.cs — Шаг 2
4. Обновить HttpServer.cs — Шаг 3
5. Обновить HTML (index.html) — Шаг 5
6. Создать JS модуль (models.js) — Шаг 6
7. Обновить CSS (style.css) — Шаг 7
8. Обновить app.js — Шаг 8
9. Добавить миграцию — Шаг 9
10. Протестировать — Шаг 10
