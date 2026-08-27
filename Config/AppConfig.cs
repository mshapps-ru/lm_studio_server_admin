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
}

