# ToDo лист для реализации вкладки **Models**

1. **Анализ существующего кода** – изучить `AppConfig`, `ModelListUpdater`, `HttpServer`.
2. **Добавление новых API‑эндпоинтов**:
   - GET `/api/models/list`
   - GET `/api/modelparams`
   - POST `/api/modelparams/default`
   - POST `/api/modelparams/model/:name`
3. **Создание контроллера** `ModelParamsController` (или расширить `SettingsController`).
4. **Расширение маршрутов** в `HttpServer.cs`.
5. **Разработка фронтенд‑компонента `Models`:**
   - Добавить `<div id="models-tab" class="tab-content"></div>` в `index.html`.
   - Реализовать модуль в `wwwroot/js/app.js` с методами `init`, `loadData`, `renderDefaultParams`, `renderModelList` и обработчиками сохранения.
6. **Добавление стилей** (при необходимости) в `wwwroot/css/style.css`.
7. **Тестирование функционала** – проверка отображения, редактирования и сохранения параметров.
8. **Обновление документации:**
   - Добавить раздел о новых эндпоинтах в `docs/project-overview.md`.
   - Создать страницу `docs/areas/models-page.md` с описанием UI.
