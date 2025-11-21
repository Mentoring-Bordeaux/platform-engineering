public class TemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, string> Parameters { get; set; } = new();
}
public class StaticWebSiteRequest : TemplateRequest
{
}