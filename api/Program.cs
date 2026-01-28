using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http.HttpResults;
using Octokit.Internal;
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
        builder.Services.AddSingleton<CreateProjectManager>();

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

        int ProjectIdCounter = 0;
        var createProjectHandler = async (
            CreateProjectRequest request,
            CreateProjectManager createProjectManager,
            PulumiService pulumiService
        ) =>
        {
            int currentId = Interlocked.Increment(ref ProjectIdCounter);
            createProjectManager.CreateNewProject(currentId, request, pulumiService, app);
            return Results.Accepted(
                "/create-project/status/" + currentId,
                new RequestReceivedResponse(request.ProjectName, currentId)
            );
        };

        // Azure Static Web Apps proxies backend requests via the fixed /api prefix.
        app.MapPost("/create-project", createProjectHandler);
        app.MapPost("/api/create-project", createProjectHandler);

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

        app.MapGet("/templates", getTemplates);
        app.MapGet("/api/templates", getTemplates);

        app.Run();
    }
}
