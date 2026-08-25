# ТЗ: Приложение администрирования LM Studio Server (Windows Service)

## 1. Общие сведения

| Параметр       | Значение                                                                 |
|----------------|--------------------------------------------------------------------------|
| **Язык**       | C# (.NET 8+)                                                             |
| **Тип приложения** | Консольное приложение, способное работать как служба Windows           |
| **Режим службы** | Запуск с параметром `-service`                                           |
| **HTTP-порт**  | 7778 (по умолчанию, настраиваемый)                                       |
| **Файл настроек** | `config.json` рядом с исполняемым файлом                              |
| **Управление LM Studio** | Команды `lms server start/stop/status` через `Process.Start`       |

---

## 2. Архитектура

```
LmStudioServerAdmin/
├── LmStudioServerAdmin.csproj
├── Program.cs                  # Точка входа, определение режима (-service)
├── Config/
│   ├── AppConfig.cs            # Модель настроек
│   └── ConfigManager.cs        # Чтение/запись config.json
├── Service/
│   ├── WindowsService.cs       # Реализация BackgroundService / ServiceBase
│   └── StatusChecker.cs        # Таймер проверки статуса LM Studio (1 мин)
├── Server/
│   ├── HttpServer.cs           # Встроенный HTTP-сервер (HttpListener или Kestrel)
│   ├── AuthManager.cs          # Проверка логина/пароля
│   ├── HomeController.cs       # Обработка запросов вкладки Home
│   └── SettingsController.cs   # Обработка запросов вкладки Settings
├── Commands/
│   └── LmsCommandExecutor.cs   # Выполнение команд lms server start/stop/status
├── wwwroot/
│   ├── index.html              # Главная страница (SPA)
│   ├── css/
│   │   └── style.css
│   ├── js/
│   │   ├── app.js              # Основная логика SPA
│   │   ├── auth.js             # Логика авторизации
│   │   ├── home.js             # Логика вкладки Home
│   │   └── settings.js         # Логика вкладки Settings
│   └── icons/                  # Иконки (опционально)
└── config.json                 # Файл настроек (создаётся автоматически)
```

---

## 3. Этапы разработки

### Этап 1: Инициализация проекта

**Задачи:**
- [ ] 1.1 Создать solution и проект консольного приложения `LmStudioServerAdmin`
- [ ] 1.2 Настроить `csproj` с целевой платформой `win-x64` (publish single file)
- [ ] 1.3 Создать структуру папок согласно архитектуре выше
- [ ] 1.4 Добавить NuGet-пакеты:
  - `Microsoft.Extensions.Hosting.WindowsServices` — для работы как служба
  - `Newtonsoft.Json` или `System.Text.Json` — для работы с JSON
- [ ] 1.5 Создать модель `AppConfig.cs`:

```csharp
public class AppConfig
{
    public string Username { get; set; } = "admin";
    public string Password { get; set; } = "admin";
    public int Port { get; set; } = 7778;
}
```

- [ ] 1.6 Создать `ConfigManager.cs` — чтение/запись `config.json` рядом с exe
- [ ] 1.7 Написать `Program.cs` с определением режима запуска:

```csharp
var isService = args.Contains("-service");
if (isService)
{
    // Запуск как служба Windows
    ServiceBase.Run(new WindowsService());
}
else
{
    // Запуск как консольное приложение
    RunConsole();
}
```

- [ ] 1.8 Создать начальный `config.json` с настройками по умолчанию

---

### Этап 2: HTTP-сервер и авторизация

**Задачи:**
- [ ] 2.1 Реализовать `HttpServer.cs` на базе `HttpListener` (без ASP.NET, лёгкий вариант):
  - Слушать порт из `config.json`
  - Разделять запросы по путям: `/login`, `/api/*`, `/static/*`
- [ ] 2.2 Реализовать `AuthManager.cs`:
  - Хранение хеша пароля (SHA-256 или BCrypt)
  - Метод `bool Authenticate(string username, string password)`
  - Сессия авторизации (простой token-based подход через cookie или header)
- [ ] 2.3 Создать страницу авторизации `wwwroot/index.html` с формой:
  - Поле "Имя пользователя"
  - Поле "Пароль"
  - Кнопка "Войти"
  - Обработка ошибок авторизации
- [ ] 2.4 Настроить редирект:
  - Неавторизованный пользователь → страница `/login`
  - Авторизованный пользователь → страница `/home`

---

### Этап 3: SPA — Вкладка "Home"

**Задачи:**
- [ ] 3.1 Создать основную страницу приложения с двумя вкладками (Home / Settings):
  - Навигация между вкладками через JavaScript
  - Кнопка "Exit" для выхода из админки (закрытие сессии и редирект на /login)
- [ ] 3.2 Реализовать `HomeController.cs` — API для вкладки Home:

| Метод   | Путь              | Описание                                      |
|---------|-------------------|-----------------------------------------------|
| GET     | `/api/status`     | Текущий статус LM Studio Server               |
| POST    | `/api/start`      | Запустить LM Studio Server                    |
| POST    | `/api/stop`       | Остановить LM Studio Server                    |

- [ ] 3.3 Реализовать `LmsCommandExecutor.cs`:

```csharp
public enum LmsStatus
{
    Running,
    Stopped,
    Unknown,
    Error
}

public class LmsCommandExecutor
{
    public LmsStatus GetStatus();       // lms server status
    public void StartServer();          // lms server start
    public void StopServer();           // lms server stop
}
```

- [ ] 3.4 Реализовать `StatusChecker.cs` — фоновый сервис:
  - Таймер на 60 секунд
  - Каждую минуту вызывает `LmsCommandExecutor.GetStatus()`
  - Результат доступен через `/api/status`
- [ ] 3.5 Реализовать `js/home.js`:
  - Отображение текущего статуса (Running / Stopped / Unknown / Error)
  - Кнопки "Start" и "Stop" с вызовом соответствующих API
  - Автоматическое обновление статуса каждые 10 секунд (polling)
  - Визуальная индикация статуса (цветные бейджи)

---

### Этап 4: SPA — Вкладка "Settings"

**Задачи:**
- [ ] 4.1 Реализовать `SettingsController.cs` — API для вкладки Settings:

| Метод   | Путь                | Описание                                        |
|---------|---------------------|-------------------------------------------------|
| GET     | `/api/settings`     | Получить текущие настройки                      |
| PUT     | `/api/settings`     | Обновить настройки                                |

- [ ] 4.2 Реализовать `js/settings.js`:
  - Форма с полями:
    - Имя пользователя (текстовое поле)
    - Пароль (поле ввода пароля)
    - Порт (числовое поле, диапазон 1-65535)
  - Валидация полей (обязательные поля, корректный порт)
  - Сохранение настроек через PUT `/api/settings`
  - Перезапуск HTTP-сервера при изменении порта (или уведомление о необходимости перезапуска)
- [ ] 4.3 При изменении настроек:
  - Обновить `config.json` через `ConfigManager`
  - Обновить пароль в `AuthManager`
  - При изменении порта — показать предупреждение о необходимости перезапуска

---

### Этап 5: Фоновая проверка статуса и полировка

**Задачи:**
- [ ] 5.1 Доработать `StatusChecker.cs`:
  - Запуск при старте приложения
  - Интервал: 60 секунд
  - Обработка ошибок (если `lms` недоступен — статус `Unknown`)
  - Кэширование последнего результата
- [ ] 5.2 Улучшить UI/UX:
  - Единый стиль для всех страниц
  - Адаптивный дизайн
  - Индикаторы загрузки при вызове API
  - Toast-уведомления об успехе/ошибке операций
- [ ] 5.3 Обработка ошибок:
  - Неверный порт (занят / недоступен)
  - Ошибка чтения `config.json`
  - Ошибка выполнения команд `lms`
  - Невозможность запуска как службы (нет прав администратора)
- [ ] 5.4 Логирование:
  - Запись логов в файл `logs/app.log`
  - Уровни: Info, Warning, Error
  - Логи работы HTTP-сервера, команд lms, ошибок

---

### Этап 6: Публикация и тестирование

**Задачи:**
- [ ] 6.1 Настроить publish-профиль для Windows:

```bash
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```

- [ ] 6.2 Протестировать:
  - Запуск из консоли (`.\LmStudioServerAdmin.exe`)
  - Запуск как службы (`.\LmStudioServerAdmin.exe -service`)
  - Авторизация с правильными/неправильными данными
  - Переключение вкладок Home / Settings
  - Управление LM Studio Server (start/stop/status)
  - Изменение настроек (логин, пароль, порт)
  - Автоматическая проверка статуса каждые 60 секунд
- [ ] 6.3 Проверить создание `config.json` при первом запуске
- [ ] 6.4 Убедиться, что служба корректно устанавливается и запускается

---

## 4. Формат ответа HTTP

### Страница авторизации
```
GET /          → 200 OK, text/html (форма входа)
POST /api/login → 200 OK, {"token": "..."}  |  401 Unauthorized
```

### Вкладка Home
```
GET  /api/status    → 200 OK, {"status": "Running", "message": "..."}
POST /api/start     → 200 OK, {"success": true}  |  500 Error
POST /api/stop      → 200 OK, {"success": true}  |  500 Error
```

### Вкладка Settings
```
GET  /api/settings  → 200 OK, {"username": "admin", "password": "", "port": 7778}
PUT  /api/settings  → 200 OK, {"success": true}  |  400 Validation Error
```

### Выход
```
POST /api/logout    → 200 OK, {"success": true}  (редирект на /login)
```

---

## 5. Файл config.json (по умолчанию)

```json
{
    "username": "admin",
    "password": "admin",
    "port": 7778
}
```

---

## 6. Технические решения

| Вопрос                              | Решение                                              |
|-------------------------------------|------------------------------------------------------|
| HTTP-сервер                         | `HttpListener` (встроенный, без дополнительных зависимостей) |
| Авторизация                         | Простой session-based через cookie с токеном         |
| Хранение пароля                     | SHA-256 хеш в config.json                            |
| Фоновая проверка статуса            | `System.Threading.Timer` или `BackgroundService`     |
| Режим службы                        | `ServiceBase.Run()` + параметр `-service`            |
| Консольный режим                    | Стандартный `Main()` с `Console.ReadLine()`          |
| SPA-навигация                       | Vanilla JS (без фреймворков, лёгкость)               |
| Стили                             | CSS с переменными, минималистичный дизайн             |

---

## 7. Критерии готовности

- [ ] Приложение запускается из консоли и открывает страницу на порту 7778
- [ ] Работает как служба Windows с параметром `-service`
- [ ] Страница авторизации требует логин/пароль
- [ ] После входа доступны вкладки Home и Settings
- [ ] Вкладка Home показывает статус LM Studio и позволяет управлять им
- [ ] Вкладка Settings позволяет менять логин, пароль и порт
- [ ] Настройки сохраняются в `config.json`
- [ ] Статус LM Studio проверяется автоматически раз в минуту
- [ ] Кнопка Exit завершает сессию и возвращает на страницу входа
