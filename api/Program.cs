using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http.HttpResults;
using Scalar.AspNetCore;
using YamlDotNet.Serialization;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

       string nuxtAppUrl =
        builder.Configuration["NuxtAppUrl"]
        ?? builder.Configuration["services:app:http:0"]
        ?? "http://localhost:3000";


        // Configure CORS
        builder.Services.AddCors(options =>
        {
            options.AddPolicy(
                "NuxtPolicy",
                policy =>
                {
                    if (builder.Environment.IsDevelopment())
                    {
                        // In development, allow any origin for convenience during local testing
                        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                    }
                    else
                    {
                        // In production, strictly allow only the configured Nuxt app origin
                        policy.WithOrigins(nuxtAppUrl).AllowAnyHeader().AllowAnyMethod();
                    }
                }
            );
        });

        // Register the Pulumi service
        builder.Services.AddScoped<PulumiService>();

        builder.Services.AddOpenApi();

        var app = builder.Build();        

        app.Logger.LogInformation(
            "Configuring CORS to allow requests from: {NuxtAppUrl}",
            nuxtAppUrl
        );

        app.UseCors("NuxtPolicy");

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference();
        }

        app.UseHttpsRedirection();

        var createProjectHandler = async (
            CreateProjectRequest request,
            PulumiService pulumiService,
            ILogger<Program> logger
        ) =>
        {
            // Validate input
            var inputError = ValidateCreateProjectRequest(request);
            if (inputError != null)
            {
                return Results.BadRequest(inputError);
            }

            try
            {
                // Prepare injected credentials
                var injectedCredentials = new Dictionary<string, string>();
                IGitRepositoryService? gitService = null;

                if (request.Platform != null && !string.IsNullOrWhiteSpace(request.Platform.Type))
                {
                    var platformType = request.Platform.Type.Trim().ToLowerInvariant();

                    if (platformType == "github")
                    {
                        var githubToken = app.Configuration["GitHubToken"];
                        var githubOrg = app.Configuration["GitHubOrganizationName"];

                        if (!HasRealConfigValue(githubToken) || !HasRealConfigValue(githubOrg))
                        {
                            return Results.BadRequest(new ResultPulumiAction
                            {
                                Name = request.ProjectName,
                                ResourceType = "GitHub",
                                StatusCode = 400,
                                Message = "GitHubToken or GitHubOrganizationName is missing in configuration."
                            });
                        }

                        injectedCredentials["githubToken"] = githubToken!;
                        injectedCredentials["githubOrganizationName"] = githubOrg!;

                        gitService = new GitHubService(
                            githubToken!,
                            githubOrg!,
                            request.ProjectName
                        );
                    }
                    else if (platformType == "gitlab")
                    {
                        var gitlabToken = app.Configuration["GitLabToken"];
                        var gitlabBaseUrl = app.Configuration["GitLabBaseUrl"];

                        if (!HasRealConfigValue(gitlabToken) || !HasValidHttpUrl(gitlabBaseUrl))
                        {
                            return Results.BadRequest(new ResultPulumiAction
                            {
                                Name = request.ProjectName,
                                ResourceType = "GitLab",
                                StatusCode = 400,
                                Message = "GitLabToken or GitLabBaseUrl is missing or invalid in configuration."
                            });
                        }

                        injectedCredentials["gitlabToken"] = gitlabToken!;
                        injectedCredentials["gitlabBaseUrl"] = gitlabBaseUrl!;
                        gitService = new GitLabService(
                            gitlabToken!,
                            request.ProjectName,
                            gitlabBaseUrl
                        );
                    }
                    else
                    {
                        return Results.BadRequest(new ResultPulumiAction
                        {
                            Name = request.ProjectName,
                            ResourceType = "Unknown",
                            StatusCode = 400,
                            Message = $"Unsupported platform type: {request.Platform.Type}"
                        });
                    }
                }

                var results = new List<ResultPulumiAction>();

                // Step 1: Execute platform (if provided)
                if (request.Platform != null && !string.IsNullOrWhiteSpace(request.Platform.Type))
                {
                    logger.LogInformation(
                        "Executing platform '{PlatformType}' for project '{ProjectName}'",
                        request.Platform.Type,
                        request.ProjectName
                    );
                    var platformResult = await pulumiService.ExecutePlatformAsync(
                        request,
                        injectedCredentials,
                        gitService!
                    );
                    results.Add(platformResult);

                    if (platformResult.StatusCode >= 400)
                    {
                        logger.LogError(
                            "Platform execution failed: {Message}",
                            platformResult.Message
                        );
                        return Results.Json(results, statusCode: platformResult.StatusCode);
                    }
                    string? gitUrl = platformResult.Outputs?.TryGetValue("repoUrl", out var repoUrlObj) == true
                                ? repoUrlObj?.ToString()
                                : null;

                    if (!string.IsNullOrEmpty(gitUrl))
                    {
                        if (request.Platform.Type.Trim().ToLowerInvariant() == "gitlab")
                            gitService = new GitLabService(injectedCredentials["gitlabToken"], gitUrl, injectedCredentials["gitlabBaseUrl"]);

                        // Étape 2 : initialiser les frameworks
                        var frameworks = request.Parameters
                                           .Where(kv => kv.Key.Contains("framework", StringComparison.OrdinalIgnoreCase))
                                           .Select(kv => Enum.TryParse<FrameworkType>(kv.Value?.ToString() ?? "", true, out var fw) ? fw : (FrameworkType?)null)
                                           .Where(fw => fw.HasValue)
                                           .Select(fw => fw!.Value)
                                           .ToList();

                        if (frameworks.Any())
                            await pulumiService.InitializeRepo(request.ProjectName!, frameworks, gitService!);
                    }
                }

                // Step 2: Execute template
                logger.LogInformation(
                    "Executing template '{TemplateName}' for project '{ProjectName}'",
                    request.TemplateName,
                    request.ProjectName
                );
                var templateResult = await pulumiService.ExecuteTemplateAsync(
                    request,
                    gitService!
                );
                results.Add(templateResult);

                if (templateResult.StatusCode >= 400)
                {
                    logger.LogError("Template execution failed: {Message}", templateResult.Message);
                    return Results.Json(results, statusCode: templateResult.StatusCode);
                }

                logger.LogInformation(
                    "Project created successfully: {ProjectName}",
                    request.ProjectName
                );
                return Results.Ok(results);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Unhandled exception in createProjectHandler: {Message}",
                    ex.Message
                );
                return Results.Json(
                    new ResultPulumiAction
                    {
                        Name = request.ProjectName,
                        ResourceType = "unknown",
                        StatusCode = 500,
                        Message = $"Internal server error: {ex.Message}",
                    },
                    statusCode: 500
                );
            }
        };

        var getTemplates = (ILogger<Program> logger) =>
        {
            var templatesDir = Path.Combine(
                Directory.GetCurrentDirectory(),
                "pulumiPrograms/templates"
            );

            if (!Directory.Exists(templatesDir))
            {
                logger.LogWarning("Templates directory not found at: {TemplatesDir}", templatesDir);
                return Results.Ok(new List<object>());
            }

            var templates = new List<object>();
            var templateDirs = Directory.GetDirectories(templatesDir);

            foreach (var templateDir in templateDirs)
            {
                var templateYamlPath = Path.Combine(templateDir, "template.yaml");

                if (!File.Exists(templateYamlPath))
                {
                    logger.LogWarning("template.yaml not found in: {TemplateDir}", templateDir);
                    continue;
                }

                try
                {
                    var yamlContent = File.ReadAllText(templateYamlPath);
                    var deserializer = new DeserializerBuilder().Build();
                    var yamlObject = deserializer.Deserialize<object>(yamlContent);

                    templates.Add(yamlObject);
                    logger.LogInformation(
                        "Successfully loaded template: {TemplateName}",
                        Path.GetFileName(templateDir)
                    );
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error loading template from {TemplateDir}", templateDir);
                }
            }
            return Results.Ok(templates);
        };

        // Azure Static Web Apps proxies backend requests via the fixed /api prefix.
        app.MapPost("/create-project", createProjectHandler);
        app.MapPost("/api/create-project", createProjectHandler);

        app.MapGet("/templates", getTemplates);
        app.MapGet("/api/templates", getTemplates);

        app.Run();
    }

    private static ResultPulumiAction? ValidateCreateProjectRequest(CreateProjectRequest request)
    {
        if (request == null)
        {
            return new ResultPulumiAction
            {
                Name = "",
                ResourceType = "",
                StatusCode = 400,
                Message = "Request body is null",
            };
        }

        if (string.IsNullOrWhiteSpace(request.TemplateName))
        {
            return new ResultPulumiAction
            {
                Name = request.ProjectName ?? "NotSpecified",
                ResourceType = "unknown",
                StatusCode = 400,
                Message = "Missing 'TemplateName' in request",
            };
        }

        if (string.IsNullOrWhiteSpace(request.ProjectName))
        {
            return new ResultPulumiAction
            {
                Name = "NotSpecified",
                ResourceType = request.TemplateName,
                StatusCode = 400,
                Message = "Missing 'ProjectName' in request",
            };
        }

        return null;
    }

    private static bool HasRealConfigValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();
        var lower = trimmed.ToLowerInvariant();
        if (
            lower.Contains("should be set")
            || lower.StartsWith("optional")
            || lower.Contains("replace_with")
        )
        {
            return false;
        }

        return true;
    }

    private static bool HasValidHttpUrl(string? value)
    {
        if (!HasRealConfigValue(value))
        {
            return false;
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            return false;
        }

        return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
    }
}
