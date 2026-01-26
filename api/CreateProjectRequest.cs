
public class CreateProjectRequest
{
    public string TemplateName { get; set; } = string.Empty;

    public Dictionary<string, object> Parameters { get; set; } = new();

    public PlatformConfig? Platform { get; set; }

    public string ProjectName { get; set; } = string.Empty;
}

public class PlatformConfig
{
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, object> Config { get; set; } = new();
}
