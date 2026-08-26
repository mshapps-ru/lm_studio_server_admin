Разработай план доработок:
1) Проанализируй документацию по текущему проекту в папке docs, при необходимости посмотри код проекта и не приступай к следующим пунктам пока все не изучишь
2) Задавай при необходимости вопросы и отображай ответы в плане
3) Добавь отображение на странице "Home" на каком порту запущен сервис lm studio server. Определи по команде lms server stop или lms server start
4) Добавь хранение порта lm studio server в памяти и в файле config.json
5) Разработай прокси для пересылки следующих запросов в сервис lm studio server для запросов с другого компьютера. Ниже список доступных эндпоинтов из лога. Подробности посмотри на сайте https://lmstudio.ai/docs/developer/rest

[2026-08-26 14:20:05][INFO][LM STUDIO SERVER] Supported endpoints:
[2026-08-26 14:20:05][INFO][LM STUDIO SERVER]   LM Studio API
[2026-08-26 14:20:05][INFO][LM STUDIO SERVER]    ->  GET  http://localhost:1234/api/v1/models
[2026-08-26 14:20:05][INFO][LM STUDIO SERVER]    ->  POST http://localhost:1234/api/v1/chat
[2026-08-26 14:20:05][INFO][LM STUDIO SERVER]    ->  POST http://localhost:1234/api/v1/models/load
[2026-08-26 14:20:05][INFO][LM STUDIO SERVER]    ->  POST http://localhost:1234/api/v1/models/download
[2026-08-26 14:20:05][INFO][LM STUDIO SERVER]    ->  GET http://localhost:1234/api/v1/models/download/status:job_id
[2026-08-26 14:20:05][INFO][LM STUDIO SERVER]   OpenAI-compatible
[2026-08-26 14:20:05][INFO][LM STUDIO SERVER]    ->  GET  http://localhost:1234/v1/models
[2026-08-26 14:20:05][INFO][LM STUDIO SERVER]    ->  POST http://localhost:1234/v1/responses
[2026-08-26 14:20:05][INFO][LM STUDIO SERVER]    ->  POST http://localhost:1234/v1/chat/completions
[2026-08-26 14:20:05][INFO][LM STUDIO SERVER]    ->  POST http://localhost:1234/v1/completions
[2026-08-26 14:20:05][INFO][LM STUDIO SERVER]    ->  POST http://localhost:1234/v1/embeddings


6) Сделай ToDо лист чтобы потом идти по плану при реализации
7) Сохрани подробный план с конкретными шагами без обрезания в файл tz/02-create-lm-studio-proxy.md только после того как у меня все ответы на вопросы получишь
8) Не приступай к доработкам пока я плане не посмотрю
9) Проверь что каждый пункт текущего промта есть в плане доработок
10) Этот промт сохрани в без обрезания в файл tz/02-create-lm-studio-proxy-prompt.md
