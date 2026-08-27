# Prompt for Adding Verbose Logging Switch

The user wants a detailed plan and ToDo list for adding a toggle switch in the Settings tab that controls the `verboseLogging` flag in `AppConfig`.  The plan should:
1. Summarise current architecture and code paths.
2. List questions that need clarification before proceeding.
3. Propose concrete changes to API endpoints, UI, config handling, and any runtime usage.
4. Provide a ToDo list of tasks.

The user also wants the plan saved in `tz/03-create-switcher-for-logging.md` and the ToDo list in `tz/03-create-switcher-for-logging-todo.md`.  Finally, the prompt itself should be persisted unchanged in `tz/03-create-switcher-for-logging-prompt.md`.

The user has requested not to start implementing changes until the plan is reviewed. The plan must include all points from the original request (review docs, ask questions, add toggle, store flag, create ToDo list, ensure plan contains every prompt item).


Разработай план доработок:
1) Проанализируй документацию по текущему проекту в папке docs, при необходимости посмотри код проекта и не приступай к следующим пунктам пока все не изучишь
2) Задавай при необходимости вопросы и отображай ответы в плане
3) Добавь на странице "Settings" новый переключатель-тумблер на редактирование параметра "VerboseProxyLogging" в файле config.json.
4) Добавь хранение параметра "VerboseProxyLogging" в памяти и в файле config.json
5) Сделай ToDо лист чтобы потом идти по плану при реализации
6) Сохрани подробный план с конкретными шагами без обрезания в файл tz/03-create-switcher-for-logging.md только после того как у меня все ответы на вопросы получишь.
7) Не приступай к доработкам пока я плане не посмотрю
8) Проверь что каждый пункт текущего промта есть в плане доработок
9) Этот промт сохрани в без обрезания в файл tz/03-create-switcher-for-logging-prompt.md
10) Согласованный ToDо лист сохрани в файл tz/03-create-switcher-for-logging-todo.md