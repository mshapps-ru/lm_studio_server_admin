namespace LmStudioServerAdmin.Config;

public class AppConfig
{
    public string Username { get; set; } = "admin";
    public string Password { get; set; } = "admin";
    public int Port { get; set; } = 7778;
    public int LmStudioPort { get; set; } = 1234;
    public bool VerboseProxyLogging { get; set; } = false;
    public string BindAddress { get; set; } = "0.0.0.0";
}
