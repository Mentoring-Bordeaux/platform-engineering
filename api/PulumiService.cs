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
        Console.WriteLine($"Received request to create resource: {request.Name}");

        Console.WriteLine($"Determining resource type from request parameters... {string.Join(", ", request.Parameters.Select(kv => $"{kv.Key}={kv.Value}"))}");

        if (!request.Parameters.TryGetValue("type", out var resourceType) || !_resourceWorkDirs.ContainsKey(resourceType)){
            Console.WriteLine("Invalid or missing resource type in request parameters.");
            return Results.BadRequest("Invalid or missing resource type");
        }

        string workingDir = _resourceWorkDirs[resourceType];
        
        string requestAndType = $"{request.Name}-{resourceType}";

        string stackName = Regex.Replace(requestAndType.ToLower(), @"[^a-z0-9\-]", "-");

        Console.WriteLine($"Using stack name: {stackName}");

        WorkspaceStack? stack = null;
        try
        {
            stack = await LocalWorkspace.CreateOrSelectStackAsync(new LocalProgramArgs(stackName, workingDir));

            await stack.SetConfigAsync("Name", new ConfigValue(request.Name));

            // Configurate the stack with parameters from the request (excluding "type")
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
            return Results.Problem(ex.Message, statusCode: 500);
        }
    }

}
