using System.Text.Json;
using Pulumi.Automation;
using Microsoft.Extensions.Logging;

public class PulumiService
{
    private readonly ILogger<PulumiService> _logger;

    public PulumiService(ILogger<PulumiService> logger)
    {
        _logger = logger;
    }

    public async Task<List<ResultPulumiAction>> ExecuteProjectAsync(
        string projectName,
        List<TemplateRequest> requests)
    {
        List<ResultPulumiAction> results = new();

        string workingDirRoot = Path.Combine(
            Directory.GetCurrentDirectory(),
            "pulumiPrograms",
            "project"
        );

        string stackName = projectName.ToLower().Replace(" ", "-");

        var stack = await LocalWorkspace.CreateOrSelectStackAsync(
            new LocalProgramArgs(stackName, workingDirRoot)
        );

        _logger.LogInformation("Using stack: {StackName}", stackName);

        await stack.SetConfigAsync("projectName", new ConfigValue(projectName));

        var resourcesConfig = requests.Select(r => new
        {
            name = r.Name,
            resourceType = r.ResourceType,
            parameters = r.Parameters
        }).ToList();

        await stack.SetConfigAsync(
            "resources",
            new ConfigValue(JsonSerializer.Serialize(resourcesConfig))
        );

        string rgName = $"{projectName}-rg";
        await stack.SetConfigAsync("resourceGroupName", new ConfigValue(rgName));

        try
        {
            
            var upResult = await stack.UpAsync(new UpOptions
            {
                OnStandardOutput = Console.WriteLine,
                OnStandardError = Console.Error.WriteLine
            });

            var outputs = upResult.Outputs.ToDictionary(
                kv => kv.Key,
                kv => kv.Value?.Value
            );

            foreach (var req in requests)
            {
                results.Add(new ResultPulumiAction
                {
                    Name = req.Name,
                    ResourceType = req.ResourceType,
                    StatusCode = 200,
                    Message = "Resources created successfully",
                    Outputs = outputs
                });
            }
        }
        catch (Exception ex)
        {
            foreach (var req in requests)
            {
                results.Add(new ResultPulumiAction
                {
                    Name = req.Name,
                    ResourceType = req.ResourceType,
                    StatusCode = 500,
                    Message = ex.Message
                });
            }
        }

        return results;
    }
}
