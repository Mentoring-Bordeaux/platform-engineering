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
        var workingDir = Path.Combine(Directory.GetCurrentDirectory(), "pulumiPrograms", resourceType);

        if (!Directory.Exists(workingDir))
        {
            _logger.LogWarning("Pulumi program not found for type '{ResourceType}' at path '{WorkingDir}'", resourceType, workingDir);
            return Results.BadRequest($"Pulumi program not found for type '{resourceType}' at path '{workingDir}'.");
        }

        string stackName = Regex.Replace($"{request.Name}".ToLower(), @"[^a-z0-9\-]", "-");
        _logger.LogInformation("Using stack name: {StackName}", stackName);

        try
        {
            var stack = await LocalWorkspace.CreateOrSelectStackAsync(new LocalProgramArgs(stackName, workingDir));

            // Config de base
            await stack.SetConfigAsync("Name", new ConfigValue(request.Name));
            foreach (var kv in request.Parameters.Where(kv => kv.Key != "type"))
                await stack.SetConfigAsync(kv.Key, new ConfigValue(kv.Value));

            var result = await stack.UpAsync(new UpOptions
            {
                OnStandardOutput = Console.WriteLine,
                OnStandardError = Console.Error.WriteLine,
            });

            var outputs = result.Outputs.ToDictionary(kv => kv.Key, kv => kv.Value?.Value);

            if (request.Parameters.TryGetValue("githubToken", out var token) &&
                request.Parameters.TryGetValue("githubOrganizationName", out var orgName))
            {
                var gitService = new GitHubService(token);

                string repoToPush = request.Name;

                if (!string.IsNullOrEmpty(request.Framework))
                {
                    // Initialisation dynamique via CLI selon le type de framework
                    if (Enum.TryParse<FrameworkType>(request.Framework, true, out var frameworkType))
                    {
                        await gitService.InitializeRepoWithFrameworkAsync(
                            orgName,
                            repoToPush,
                            frameworkType,
                            request.Name
                        );
                    }
                    else
                    {
                        _logger.LogWarning("Framework non supporté : {Framework}", request.Framework);
                    }
                }
                else
                {
                    // Si pas de framework, on peut éventuellement utiliser un repo cible existant
                    repoToPush = request.Parameters.ContainsKey("targetRepo") ? request.Parameters["targetRepo"] : request.Name;
                }

                // Push le code Pulumi si ce n'est pas une plateforme GitHub
                if (!resourceType.StartsWith("platforms/github"))
                {
                    if (Directory.Exists(workingDir))
                    {
                        await gitService.PushPulumiCodeAsync(
                            orgName,
                            repoToPush,
                            workingDir,
                            request.Parameters,
                            request.Name
                        );
                    }
                    else
                    {
                        _logger.LogWarning("Pulumi program path does not exist: {PulumiPath}", workingDir);
                    }
                }
            }


            return Results.Json(outputs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing Pulumi program for {ResourceType}", resourceType);
            return Results.Problem(ex.Message, statusCode: 500);
        }
    }
}
