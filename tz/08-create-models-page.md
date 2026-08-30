# План создания вкладки **Models**

## Цель
Добавить в веб‑интерфейс новую вкладку *Models*, отображающую список доступных моделей LM Studio, а также параметры их загрузки и глобальные параметры по умолчанию. Пользователь сможет редактировать существующие модели и сохранять изменения в `config.json`.

## Что уже готово
- Конфигурационный класс `AppConfig` содержит свойства:
  ```csharp
  public List<ModelInfo>? LmStudioModelList { get; set; }
  public Dictionary<string, int?>? LmStudioModelDefaultLoadParameter { get; set; }
  public List<ModelLoadParametersEntry>? LmStudioModelLoadParameterList { get; set; }
  ```
- `Service/ModelListUpdater` периодически обновляет `LmStudioModelList` из `/v1/models`.
- В `wwwroot/index.html` уже есть вкладки *Home* и *Settings*.

## Шаги реализации
### 1. Исследование существующего кода
- Прочитать README, docs/ и текущие контроллеры для понимания API‑уровня.
- Проверить, как реализован `ConfigManager` (сохранение в `config.json`).

### 2. Определить новые API‑эндпоинты
| Метод | Путь | Описание |
|-------|------|----------|
| GET | `/api/models/list` | Возвращает массив `ModelInfo` из `LmStudioModelList`. |
| GET | `/api/modelparams` | Возвращает объект с двумя полями: `defaultParams` и `modelParamsList`. |
| POST | `/api/modelparams/default` | Принимает JSON‑объект `{ contextWindow?, ttl?, ... }`, сохраняет в `LmStudioModelDefaultLoadParameter`. |
| POST | `/api/modelparams/model/:name` | Принимает параметры модели, обновляет запись в `LmStudioModelLoadParameterList`. |

- Добавить маршруты в `HttpServer.cs`. 
- Реализовать методы в новом контроллере `ModelParamsController` (или расширить `SettingsController`).
- В каждом методе использовать `ConfigManager.Load()` / `Save(config)`.

### 3. Создать фронтенд‑компонент *Models*
- Добавить `<div id="models-tab" class="tab-content"></div>` в `index.html` между Home и Settings.
- В `wwwroot/js/app.js` добавить новый модуль `Models`:
  - `init()` – подключает события, вызывает `loadData()`.
  - `loadData()` – делает запросы `/api/models/list` и `/api/modelparams`.
  - `renderDefaultParams(params)` – выводит секцию «Default Load Parameters» с полями ввода и кнопкой «Save Defaults».
  - `renderModelList(models, paramsList)` – выводит таблицу моделей. Для каждой строки: имя модели, список параметров (клик‑тоггл для редактирования) и кнопка «Save».
- Стили добавить в `wwwroot/css/style.css` (можно использовать существующие классы `.card`, `.form-group`).

### 4. Обработчики сохранения
- Для defaults: при клике на кнопку собрать данные из полей, отправить POST `/api/modelparams/default`.
- Для модели: после редактирования параметров собрать объект `{ Parameters: { ... } }`, отправить POST `/api/modelparams/model/<name>`.
- После успешного ответа обновить UI и показать сообщение через `Toast.show()`.

### 5. Тестирование
1. Запустить сервер, открыть вкладку *Models* – должна отобразиться таблица с моделями из `config.json`.
2. Изменить значение в поле defaults → «Save» → проверить обновление в файле `config.json`.
3. Перезапустить приложение и убедиться, что новые значения загружаются.
4. Попробовать изменить параметры конкретной модели – убедиться, что запись в `LmStudioModelLoadParameterList` обновлена.

### 6. Документация
- Обновить `docs/project-overview.md`, добавить раздел о новых эндпоинтах и UI‑компоненте *Models*.
- Создать страницу `docs/areas/models-page.md` с описанием пользовательского интерфейса.

## Итоговый список файлов, которые будут изменены или созданы
| Файл | Изменение |
|------|-----------|
| wwwroot/index.html | Добавлена секция *Models* |
| wwwroot/js/app.js | Добавлен модуль `Models` |
| wwwroot/css/style.css | (если требуется) новые стили |
| Server/HttpServer.cs | Расширены маршруты |
| Server/ModelParamsController.cs (или SettingsController) | Реализованы эндпоинты |
| Config/AppConfig.cs | (не меняем, но проверяем) |
| tz/08-create-models-page.md | План |
| tz/08-create-models-page-todo.md | Текущий ToDo лист |
| tz/08-create-models-page-prompt.md | Исходный запрос пользователя |
