using Microsoft.AspNetCore.Http.HttpResults;
using Scalar.AspNetCore;
using YamlDotNet.Serialization;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        string nuxtAppUrl =
            builder.Configuration["services:app:http:0"]
            ?? builder.Configuration["NuxtAppUrl"]
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

        // Register the generic Pulumi service
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
            if (string.IsNullOrWhiteSpace(request.TemplateName))
            {
                return Results.BadRequest(new { message = "TemplateName is required" });
            }

            if (string.IsNullOrWhiteSpace(request.ProjectName))
            {
                return Results.BadRequest(new { message = "ProjectName is required" });
            }


            try
            {
                // Inject GitHub credentials from configuration
                string githubToken = app.Configuration["GitHubToken"] ?? "";
                string githubOrganizationName = app.Configuration["GitHubOrganizationName"] ?? "";
                string gitlabToken = app.Configuration["GitLabToken"] ?? "";
                string gitlabBaseUrl = app.Configuration["GitLabBaseUrl"] ?? "";

                // Correction du typage : on s'assure que le dictionnaire est bien Dictionary<string, object>
                if (request.Parameters == null)
                {
                    request.Parameters = new Dictionary<string, object>();
                }

                if (HasRealConfigValue(githubToken))
                {
                    request.Parameters["githubToken"] = githubToken;
                }
                if (HasRealConfigValue(githubOrganizationName))
                {
                    request.Parameters["githubOrganizationName"] = githubOrganizationName;
                }
                if (HasRealConfigValue(gitlabToken))
                {
                    request.Parameters["gitlabToken"] = gitlabToken;
                }
                if (HasValidHttpUrl(gitlabBaseUrl))
                {
                    request.Parameters["gitlabBaseUrl"] = gitlabBaseUrl;
                }

                IResult result = await pulumiService.ExecuteTemplateAsync(request);

                if (result is JsonHttpResult<Dictionary<string, object>> jsonResult)
                {
                    logger.LogInformation(
                        "Project created successfully: {ProjectName} using {TemplateName}",
                        request.ProjectName,
                        request.TemplateName
                    );
                    return Results.Ok(
                        new
                        {
                            message = "Project created successfully",
                            outputs = jsonResult.Value,
                        }
                    );
                }

                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating project from template");
                return Results.Problem(ex.Message, statusCode: 500);
            }
        };

        // Azure Static Web Apps proxies backend requests via the fixed /api prefix.
        app.MapPost("/create-project", createProjectHandler);
        app.MapPost("/api/create-project", createProjectHandler);

        app.MapGet(
            "/templates",
            (ILogger<Program> logger) =>
            {
                var templatesDir = Path.Combine(Directory.GetCurrentDirectory(), "templates");

                if (!Directory.Exists(templatesDir))
                {
                    logger.LogWarning(
                        "Templates directory not found at: {TemplatesDir}",
                        templatesDir
                    );
                    return Results.Ok(new List<object>());
                }

                var templates = new List<object>();

                // Get all subdirectories in templates
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
                        logger.LogError(
                            ex,
                            "Failed to parse template file: {TemplateDir}",
                            templateDir
                        );
                    }
                }

                return Results.Ok(templates);
            }
        );

        app.Run();
    }
    private static bool HasRealConfigValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        // Prevent common placeholder strings from being treated as real configuration.
        // This avoids injecting invalid URLs (like "optional (self-hosted GitLab): ...") into Pulumi providers.
        var trimmed = value.Trim();
        var lower = trimmed.ToLowerInvariant();
        if (lower.Contains("should be set") || lower.StartsWith("optional") || lower.Contains("replace_with"))
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
