# TODO: Создание страницы управления моделями

## Фаза 1: Backend — Модели данных

- [ ] **1.1** Обновить `Config/AppConfig.cs`:
  - [ ] Добавить enum `ParameterType` (Int, Double, Bool, String)
  - [ ] Добавить класс `ModelParameter` (Value: string, Type: ParameterType)
  - [ ] Изменить `Parameters` в `ModelLoadParametersEntry` на `Dictionary<string, ModelParameter>?`
  - [ ] Изменить `LmStudioModelDefaultLoadParameter` на `Dictionary<string, ModelParameter>?`
  - [ ] Добавить helper-методы для конвертации типов
  - [ ] Добавить метод `MigrateConfig()` для обратной совместимости со старым форматом int?

## Фаза 2: Backend — Команды

- [ ] **2.1** Обновить `Commands/LmsCommandExecutor.cs`:
  - [ ] Добавить публичный метод `UnloadModelPublic(string modelName)` для выгрузки конкретной модели
  - [ ] Либо сделать существующий приватный `UnloadModel` публичным

## Фаза 3: Backend — Контроллер

- [ ] **3.1** Создать `Server/ModelController.cs`:
  - [ ] GET `/api/models` — получить список моделей
  - [ ] GET `/api/models/parameters/default` — получить дефолтные параметры
  - [ ] PUT `/api/models/parameters/default` — сохранить дефолтные параметры
  - [ ] GET `/api/models/parameters/{modelName}` — получить параметры модели
  - [ ] PUT `/api/models/parameters/{modelName}` — сохранить параметры модели
  - [ ] POST `/api/models/{modelName}/load` — загрузить модель
  - [ ] POST `/api/models/{modelName}/unload` — выгрузить модель
  - [ ] POST `/api/models/refresh` — обновить список моделей из LM Studio

## Фаза 4: Backend — Маршрутизация

- [ ] **4.1** Обновить `Server/HttpServer.cs`:
  - [ ] Добавить маршруты для всех new endpoints из ModelController
  - [ ] Обработать path parameters для `{modelName}` в URL
  - [ ] Добавить логирование запросов к новым endpoints

## Фаза 5: Frontend — HTML

- [ ] **5.1** Обновить `wwwroot/index.html`:
  - [ ] Добавить кнопку вкладки `<button class="tab" data-tab="models">Models</button>`
  - [ ] Добавить контейнер `<div id="models-tab" class="tab-content">` с:
    - [ ] Карточкой списка моделей с кнопкой "Refresh"
    - [ ] Карточкой дефолтных параметров
  - [ ] Добавить `<script src="/js/models.js"></script>` в body

## Фаза 6: Frontend — JavaScript

- [ ] **6.1** Создать `wwwroot/js/models.js`:
  - [ ] Объект `Models` с методом `init()`
  - [ ] Метод `loadModels()` — загрузка списка из `/api/models`
  - [ ] Метод `renderModelsList(models)` — рендеринг карточек моделей
  - [ ] Метод `loadModelParameters(modelName)` — загрузка параметров
  - [ ] Метод `renderModelParameters(modelName, parameters)` — рендеринг редактора
  - [ ] Метод `loadDefaultParameters()` — загрузка дефолтных
  - [ ] Метод `renderDefaultParameters(parameters)` — рендеринг редактора
  - [ ] Метод `saveModelParameters(modelName)` — сохранение параметров модели
  - [ ] Метод `saveDefaultParameters()` — сохранение дефолтных
  - [ ] Метод `loadModel(modelName)` — вызов Load API
  - [ ] Метод `unloadModel(modelName)` — вызов Unload API
  - [ ] Метод `refreshModels()` — обновление списка
  - [ ] Функция добавления/удаления параметров в редакторе
  - [ ] Обработка разных типов параметров (int, double, bool, string)

- [ ] **6.2** Обновить `wwwroot/js/app.js`:
  - [ ] Добавить вызов `Models.init()` в метод `showApp()`

## Фаза 7: Frontend — CSS

- [ ] **7.1** Обновить `wwwroot/css/style.css`:
  - [ ] Стили для `.model-card`
  - [ ] Стили для `.model-header`, `.model-owned-by`
  - [ ] Стили для `.model-actions`
  - [ ] Стили для `.param-row`
  - [ ] Стили для редактора параметров
  - [ ] Стили для кнопки удаления параметра
  - [ ] Стили для loading spinner

## Фаза 8: Тестирование и документация

- [ ] **8.1** Тестирование:
  - [ ] Проверить загрузку списка моделей
  - [ ] Проверить отображение параметров для каждой модели
  - [ ] Проверить редактирование и сохранение параметров модели
  - [ ] Проверить редактирование и сохранение дефолтных параметров
  - [ ] Проверить кнопки Load/Unload
  - [ ] Проверить кнопку обновления списка
  - [ ] Проверить обратную совместимость с существующим config.json
  - [ ] Проверить работу с разными типами параметров
  - [ ] Проверить обработку ошибок

- [ ] **8.2** Документация:
  - [ ] Обновить `docs/areas/backend-api-structure.md` с новыми endpoints
  - [ ] Запустить `@docs-maintainer обнови docs`
