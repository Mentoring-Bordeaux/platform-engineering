public class ResultPulumiAction
{
    public string Name { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;

    public int StatusCode { get; set; }

    public string Message { get; set; } = string.Empty;

    public Dictionary<string, object>? Outputs { get; set; }
}
