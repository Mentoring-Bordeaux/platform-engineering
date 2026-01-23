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
                if (!string.IsNullOrEmpty(githubToken))
                {
                    request.Parameters["githubToken"] = githubToken;
                }

                if (!string.IsNullOrEmpty(githubOrganizationName))
                {
                    request.Parameters["githubOrganizationName"] = githubOrganizationName;
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
                        new { message = "Project created successfully", outputs = jsonResult.Value }
                    );
                }

                return result;
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
                        Name = "",
                        ResourceType = "",
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
                    logger.LogError(ex, "Error creating project from template");
                    return Results.Problem(ex.Message, statusCode: 500);
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
}
