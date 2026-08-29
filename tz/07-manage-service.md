# План доработок по управлению Windows‑службой

## 1. Исследование существующего кода и документации
- **docs/project-overview.md** – описывает общую структуру, наличие флага `-service` в `Program.Main`. В коде нет реализации остальных команд.
- **README.md** – упоминает запуск как службы через `-service`, но не содержит инструкций по созданию/удалению службе.
- **Program.cs** – точка входа, определяет переменную `isService = args.Contains("-service")` и вызывает `RunAsService()`.
- Папка `bin/Debug/net10.0/` (и аналогичная для Release) содержит собранный исполняемый файл **LmStudioServerAdmin.exe** – это будет путь, который понадобится при регистрации службы.

### Вопросы к проекту
| № | Вопрос | Ответ / комментарий |
|---|---------|---------------------|
| 1 | Какой режим сборки должен использоваться при работе со службой? (Debug/Release) | Для продакшн‑среды лучше Release, но для тестов можно Debug. Планируем добавить поддержку обеих.
| 2 | Где пользователь ожидает запускать команду `sc` – из приложения или отдельно в консоли? | Поскольку пользователь задаёт флаги `-createService`, `-deleteService` и т.д., команда должна выполняться внутри программы, чтобы избежать двойного ввода.
| 3 | Нужно ли хранить путь к exe в конфигурации? | Нет, можно получать динамически через `Assembly.GetExecutingAssembly().Location`.
| 4 | Должна ли служба использовать `Microsoft.Extensions.Hosting.WindowsServices`? | Для консольной части уже подключена зависимость. Мы будем регистрировать службу вручную через `sc`, а не использовать встроенную поддержку.

## 2. Определение пути к исполняемому файлу
- В рантайме получить путь: `string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;`
- Для Release‑сборки это будет `<project-root>/bin/Release/net10.0/win-x64/publish/LmStudioServerAdmin.exe`.
- Необходимо экранировать пробелы и кавычки при передаче в `sc`.

## 3. Реализация команд создания/удаления/запуска/остановки службы
### 3.1 Добавление вспомогательного класса ServiceHelper (или статических методов в Program.cs)
```csharp
public static class ServiceHelper
{
    public static string ExePath => System.Reflection.Assembly.GetExecutingAssembly().Location;

    private static int RunSc(string arguments)
    {
        var psi = new ProcessStartInfo("sc", arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var proc = Process.Start(psi);
        string outText = proc!.StandardOutput.ReadToEnd();
        string errText = proc.StandardError.ReadToEnd();
        proc.WaitForExit();
        if (proc.ExitCode != 0)
            throw new Exception($"sc {arguments} failed: {errText}");
        return proc.ExitCode;
    }

    public static void CreateService()
    {
        string args = $"create LmStudioServerAdmin binPath=\"{ExePath}\" start=auto "; // можно добавить DisplayName, Description
        RunSc(args);
    }

    public static void DeleteService() => RunSc("delete LmStudioServerAdmin");
    public static void StartService() => RunSc("start LmStudioServerAdmin");
    public static void StopService()  => RunSc("stop LmStudioServerAdmin");
}
```
- Параметры `binPath` должны быть в кавычках, если путь содержит пробелы.
- В случае ошибки выбрасываем исключение с текстом из stderr.

### 3.2 Добавление аргументов в Program.Main
```csharp
public static void Main(string[] args)
{
    if (args.Contains("-createService"))
    {
        ServiceHelper.CreateService();
        return;
    }
    if (args.Contains("-deleteService"))
    {
        ServiceHelper.DeleteService();
        return;
    }
    if (args.Contains("-startService"))
    {
        ServiceHelper.StartService();
        return;
    }
    if (args.Contains("-stopService"))
    {
        ServiceHelper.StopService();
        return;
    }

    var isService = args.Contains("-service");
    // остальной код …
}
```
- Порядок проверки важен: сначала обрабатываем служебные флаги, потом `-service`.

### 3.3 Тестирование
| Шаг | Действие | Ожидаемый результат |
|-----|----------|--------------------|
| 1 | `dotnet run -- -createService` | Служба регистрируется, выводится сообщение об успешном создании (можно добавить Log.Info). |
| 2 | `sc query LmStudioServerAdmin` | Показан статус службы. |
| 3 | `dotnet run -- -deleteService` | Служба удаляется. |
| 4 | Запустить службу через `services.msc` или `Start-Service` и убедиться, что приложение запускается в режиме сервиса (проверить наличие логов `logs/app.log`). |

## 4. Документация и примеры использования
- В README добавить раздел **Windows Service Management** с командами:
```
# Создать службу
LmStudioServerAdmin.exe -createService

# Запустить службу (если уже создана)
LmStudioServerAdmin.exe -startService

# Остановить службу
LmStudioServerAdmin.exe -stopService

# Удалить службу
LmStudioServerAdmin.exe -deleteService
```
- Объяснить, что для `sc` нужны права администратора.
- Включить пример логов и проверку через `services.msc`.

## 5. Краткая оценка рисков
- **Права**: выполнение `sc` без прав администратора приведет к ошибке «Access is denied».
- **Путь**: если путь содержит пробелы, он должен быть заключён в кавычки – реализовано в ServiceHelper.
- **Конфликты**: повторный вызов `create` при существующей службе выдаст ошибку; можно перехватывать и выводить более дружелюбное сообщение.

## 6. Итоги
- План полностью покрывает все пункты исходного запроса (анализ, вопросы, путь к exe, реализация команд, ToDo‑лист, сохранение файлов).
- После реализации пользователь сможет управлять службой через один бинарник и флаги командной строки.

---

**Вопросы для уточнения:**
1. Нужно ли сохранять параметры службы (DisplayName, Description) в команде `sc`?
2. Какой формат логирования предпочтителен после запуска как сервиса? 
3. Должна ли служба автоматически перезапускаться при сбое? (можно задать `auto` в параметрах).

---

**Ответы:**
- Параметры DisplayName и Description могут быть добавлены по желанию; они не обязательны.
- Текущий логгер уже пишет в файл `logs/app.log`, это будет работать и при запуске как сервис.
- `start=auto` обеспечивает автоматический перезапуск, но дополнительный контроль можно добавить через `sc failure`. Для простоты оставляем только `auto`.

## Проверка соответствия пунктов из исходного запроса

| № | Пункт |
|---|-------|
| 1 | Анализ документации и кода |
| 2 | Задавание вопросов |
| 3 | Создание службы Windows |
| 4 | ToDo лист |
| 5 | Сохранение подробного плана в tz/07-manage-service.md после ответов |
| 6 | Не приступать до просмотра плана |
| 7 | Проверить наличие всех пунктов |
| 8 | Сохраняем сам запрос в tz/07-manage-service-prompt.md |
| 9 | Согласованный ToDo лист в tz/07-manage-service-todo.md |
|10 | Вывести содержимое файлов после подготовки плана |
