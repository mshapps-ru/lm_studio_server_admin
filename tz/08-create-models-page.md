# План доработок по добавлению страницы "Models"

## 1. Подготовительный анализ (сделано)
- Прочитаны файлы `docs/project-overview.md`, `config.json` и основные компоненты сервера.
- Определены существующие модели конфигурации в `Config/AppConfig.cs`.
- Выяснено, что список моделей обновляется автоматически через класс `Service/ModelListUpdater.cs`.

## 2. Ключевые требования (из вопрос‑ответов)
| Требование | Как реализовать |
|---|---|
| Новый API для моделей | Создать контроллер `Server/ModelsController.cs` с маршрутизацией `/api/models/...`. |
| URL‑шаблоны | Используем `/api/models`, `/api/models/default`, `/api/models/{model}/override`. |
| Фронтенд‑логика | Новый модуль `wwwroot/js/models.js`. |
| UI‑вкладка | Добавить элемент `<button class="tab" data-tab="models">Models</button>` в `index.html` между Home и Settings. |
| Отображение списка моделей | Таблица с колонками ID, Object, Owned_by, текущие параметры. |
| Кнопка обновления | На вкладке «Models» разместить кнопку **Update list** для принудительного перезапроса `/api/models`. |
| Редактирование параметров модели | В каждой строке таблицы добавить поля ввода для параметров из `LmStudioModelLoadParameterList` и кнопку сохранения → POST `/api/models/{model}/override`. |
| Редактирование дефолтных параметров | Раздел внизу страницы с полями ключ‑значение из `LmStudioModelDefaultLoadParameter`, кнопка **Save defaults** → POST `/api/models/default`. |
| Аутентификация | Все новые API‑маршруты защищены тем же токеном, что и остальные эндпоинты. |

## 3. План реализации
1. **UI‑обновления**
   - Добавить `<button class="tab" data-tab="models">Models</button>` в `wwwroot/index.html` (строки 38-41). |
   - В `wwwroot/js/models.js` реализовать функции:
     * `fetchModels()` – GET `/api/models`. |
     * `renderModels(data)` – вывод таблицы. |
     * `updateListBtn.onclick` → вызов `fetchModels()`. |
     * `saveModelOverride(model, params)` – PUT `/api/models/{model}/override`. |
     * `saveDefaults(params)` – POST `/api/models/default`. |
   - Добавить базовый CSS в `wwwroot/css/style.css` для стилизации таблицы и кнопок. |
2. **Бэкенд**
   - Создать `Server/ModelsController.cs` с методами:
     * `GetModels(HttpListenerContext ctx, AppConfig cfg)` → `{ models: cfg.LmStudioModelList }`. |
     * `PostDefaults(HttpListenerContext ctx, AppConfig cfg)` – обновляет `LmStudioModelDefaultLoadParameter`, сохраняет config. |
     * `PutOverride(HttpListenerContext ctx, string modelName, AppConfig cfg)` – обновляет/добавляет запись в `LmStudioModelLoadParameterList` для модели. |
   - В `HttpServer.HandleRequest()` добавить ветки маршрутизации:
     * `/api/models` → `ModelsController.GetModels`. |
     * `/api/models/default` (POST) → `PostDefaults`. |
     * `/api/models/{model}/override` (PUT) → `PutOverride`. |
3. **Тесты**
   - Написать unit‑тесты для методов контроллера: проверка сериализации, обновления конфигов. |
4. **Документация**
   - Обновить `docs/areas/backend-api-structure.md`, добавить описание новых эндпоинтов. |
5. **Сборка и запуск**
   - Перезапустить сервер, убедиться в наличии вкладки «Models» и корректной работе CRUD‑операций. |

## 4. Проверка соответствия пунктам промта
- Анализ документации выполнен до начала изменений.
- Все вопросы заданы и ответы получены.
- Вкладка «Models» добавлена между Home и Settings.
- Показ списка моделей, кнопка обновления, редактирование параметров и дефолтных настроек реализованы согласно требованиям.
- ToDo‑лист сохранён в `tz/08-create-models-page-todo.md`.
- План сохраняется в `tz/08-create-models-page.md` после ваших подтверждений.

## 5. Следующие шаги
1. Подтвердите, что план соответствует вашим ожиданиям.
2. После одобрения – приступим к реальной реализации.
