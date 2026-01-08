public class TemplateRequest
{
    public string Name { get; set; } = string.Empty;

    public string ResourceType { get; set; } = string.Empty;
    public string? Framework { get; set; } = null;
    public Dictionary<string, string> Parameters { get; set; } = new();
}

public class ProjectRequest
{
    public string ProjectName { get; set; } = string.Empty;
    public TemplateRequest[] Resources { get; set; } = Array.Empty<TemplateRequest>();
}
