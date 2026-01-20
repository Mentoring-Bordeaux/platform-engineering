using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Pulumi.Automation;

public class PulumiService
{
    private readonly ILogger<PulumiService> _logger;

    private readonly IHostEnvironment _environment;

    public PulumiService(ILogger<PulumiService> logger, IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public async Task<IResult> ExecuteTemplateAsync(CreateProjectRequest request)
    {
        string templateName = request.TemplateName;
        var templateDir = Path.Combine(Directory.GetCurrentDirectory(), "templates", templateName);

        _logger.LogInformation(
            "Looking for template '{TemplateName}' at path '{TemplateDir}'",
            templateName,
            templateDir
        );

        if (!Directory.Exists(templateDir))
        {
            _logger.LogWarning(
                "Template not found: '{TemplateName}' at path '{TemplateDir}'",
                templateName,
                templateDir
            );
            return Results.BadRequest($"Template not found: '{templateName}'");
        }

        var pulumiProgramDir = Path.Combine(templateDir, "pulumi");

        if (!Directory.Exists(pulumiProgramDir))
        {
            _logger.LogWarning(
                "Pulumi program not found in template '{TemplateName}' at path '{PulumiDir}'",
                templateName,
                pulumiProgramDir
            );
            return Results.BadRequest($"Pulumi program not found in template '{templateName}'");
        }

        string stackName = Regex.Replace(request.ProjectName.ToLower(), @"[^a-z0-9\-]", "-");

        _logger.LogInformation(
            "Using stack name: {StackName} for template: {TemplateName}",
            stackName,
            templateName
        );

        WorkspaceStack? stack = null;
        try
        {
            stack = await LocalWorkspace.CreateOrSelectStackAsync(
                new LocalProgramArgs(stackName, pulumiProgramDir)
            );

            // Install Pulumi dependencies (required for TypeScript programs)
            if (_environment.IsDevelopment())
            {
                await stack.Workspace.InstallAsync();
            }

            await stack.SetConfigAsync("name", new ConfigValue(request.ProjectName));

            // Set all template parameters
            foreach (var kv in request.Parameters)
            {
                var valueStr = kv.Value?.ToString() ?? "";
                // For boolean parameters, Pulumi expects "true"/"false"
                // For non-boolean parameters, preserve the original casing
                string normalizedValue = bool.TryParse(valueStr, out var boolValue)
                    ? boolValue.ToString().ToLowerInvariant()
                    : valueStr;

                await stack.SetConfigAsync(kv.Key, new ConfigValue(normalizedValue));
            }

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
            _logger.LogError(ex, "Error executing template '{TemplateName}'", templateName);
            return Results.Problem(ex.Message, statusCode: 500);
        }
        finally
        {
            // Clean up the stack
            if (stack != null)
            {
                try
                {
                    await stack.DestroyAsync();
                    await stack.Workspace.RemoveStackAsync(stackName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to clean up stack '{StackName}'", stackName);
                }
            }
        }
    }
}
