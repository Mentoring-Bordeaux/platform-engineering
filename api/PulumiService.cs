using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Pulumi.Automation;

public class PulumiService
{
    private readonly ILogger<PulumiService> _logger;

    public PulumiService(ILogger<PulumiService> logger)
    {
        _logger = logger;
    }

    public async Task<IResult> ExecuteAsync(TemplateRequest request)
    {
        string resourceType = request.ResourceType;
        var workingDir = Path.Combine(
            Directory.GetCurrentDirectory(),
            "pulumiPrograms",
            resourceType
        );

        _logger.LogInformation(
            "Looking for Pulumi program for type '{ResourceType}' at path '{WorkingDir}'",
            resourceType,
            workingDir
        );

        if (!Directory.Exists(workingDir))
        {
            _logger.LogWarning(
                "Pulumi program not found for type '{ResourceType}' at path '{WorkingDir}'",
                resourceType,
                workingDir
            );
            return Results.BadRequest(
                $"Pulumi program not found for type '{resourceType}' at path '{workingDir}'."
            );
        }

        string requestAndType = $"{request.Name}-{resourceType}";

        string stackName = Regex.Replace(requestAndType.ToLower(), @"[^a-z0-9\-]", "-");

        _logger.LogInformation("Using stack name: {StackName}", stackName);

        WorkspaceStack? stack = null;
        try
        {
            stack = await LocalWorkspace.CreateOrSelectStackAsync(
                new LocalProgramArgs(stackName, workingDir)
            );

            await stack.SetConfigAsync("Name", new ConfigValue(request.Name));

            // Configurate the stack with parameters from the request (excluding "type")
            foreach (var kv in request.Parameters.Where(kv => kv.Key != "type"))
                await stack.SetConfigAsync(kv.Key, new ConfigValue(kv.Value));

            var result = await stack.UpAsync(
                new UpOptions
                {
                    OnStandardOutput = Console.WriteLine,
                    OnStandardError = Console.Error.WriteLine,
                }
            );

            var outputs = result.Outputs.ToDictionary(kv => kv.Key, kv => kv.Value?.Value);
            return Results.Json(outputs);
        }
        catch (Exception ex)
        {
            return Results.Problem(ex.Message, statusCode: 500);
        }
    }
}
