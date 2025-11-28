using Pulumi.Automation;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

public class PulumiService
{
    private readonly Dictionary<string, string> _resourceWorkDirs = new()
{
    { "github-repo", Path.Combine(Directory.GetCurrentDirectory(), "pulumiPrograms", "github") },
    { "static-webapp", Path.Combine(Directory.GetCurrentDirectory(), "pulumiPrograms", "staticWebApp") }
};

public async Task<IResult> ExecuteAsync(TemplateRequest request)
{
    if (!request.Parameters.TryGetValue("type", out var resourceType) || !_resourceWorkDirs.ContainsKey(resourceType))
        return Results.BadRequest("Invalid or missing resource type");

    var workingDir = _resourceWorkDirs[resourceType];
    

    string stackName = Regex.Replace(request.Name.ToLower(), @"[^a-z0-9\-]", "-");

    WorkspaceStack? stack = null;
    try
    {
        stack = await LocalWorkspace.CreateOrSelectStackAsync(new LocalProgramArgs(stackName, workingDir));

        // Configurer tous les paramètres (sauf 'type')
        foreach (var kv in request.Parameters.Where(kv => kv.Key != "type"))
            await stack.SetConfigAsync(kv.Key, new ConfigValue(kv.Value));

        var result = await stack.UpAsync(new UpOptions
        {
            OnStandardOutput = Console.WriteLine,
            OnStandardError = Console.Error.WriteLine
        });

        var outputs = result.Outputs.ToDictionary(kv => kv.Key, kv => kv.Value?.Value);
        return Results.Json(outputs);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine(ex);
        return Results.Problem(ex.Message, statusCode: 500);
    }
}

}
