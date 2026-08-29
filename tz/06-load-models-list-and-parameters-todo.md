1. Define DTO classes: ModelInfo, DefaultLoadParameter, IndividualLoadParameter.
2. Extend AppConfig.cs with LmStudioModelList, LmStudioModelDefaultLoadParameter, LmStudioModelLoadParameterList.
3. Create background service ModelUpdater.cs to fetch /v1/models every 5 minutes and update config if changed.
4. Implement FetchModelsAsync() using HttpClient.
5. Hook ModelUpdater.StartAsync into Program.cs after loading config.
6. Update LmsCommandExecutor.LoadModel() to build command args from defaults + overrides.
7. Add unit tests for fetch logic, change detection, and parameter resolution.
8. Write integration test to verify periodic update writes to config.json.
9. Update docs (backend-api-structure.md) to include new background task description.
10. Verify logger outputs on errors during fetching.