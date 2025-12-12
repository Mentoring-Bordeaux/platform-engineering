public class TemplateRequest
{
    public string Name { get; set; } = string.Empty;

    public string ResourceType { get; set; } = string.Empty;
    public string? Framework { get; set; } = null;
    public Dictionary<string, string> Parameters { get; set; } = new();
}
