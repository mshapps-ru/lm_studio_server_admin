namespace LmStudioServerAdmin.Config;

public class AppConfig
{
    public string Username { get; set; } = "admin";
    public string Password { get; set; } = "admin";
    public int Port { get; set; } = 7778;
    public int LmStudioPort { get; set; } = 1234;
    public bool VerboseLogging { get; set; } = false;
    public string BindAddress { get; set; } = "0.0.0.0";
    // Persist name of currently loaded model in LM Studio
    public string LmStudioLoadedModel { get; set; } = "";

    // New properties for model list and parameters
    public List<ModelInfo>? LmStudioModelList { get; set; }
    public Dictionary<string, int?>? LmStudioModelDefaultLoadParameter { get; set; }
    public List<ModelLoadParametersEntry>? LmStudioModelLoadParameterList { get; set; }
}

public class ModelInfo
{
    public string Id { get; set; } = "";
    public string Object { get; set; } = "";
    public string Owned_by { get; set; } = "";
}

public class ModelLoadParametersEntry
{
    public string Model { get; set; } = "";
    // Additional optional parameters as key-value pairs
    public Dictionary<string, int?>? Parameters { get; set; }
}