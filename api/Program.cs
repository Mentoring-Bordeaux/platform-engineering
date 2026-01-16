using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http.HttpResults;
using Scalar.AspNetCore;

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
            TemplateRequest[] request,
            PulumiService pulumiService,
            ILogger<Program> logger
        ) =>
        {
            List<ResultPulumiAction> results = new List<ResultPulumiAction>();
            foreach (TemplateRequest req in request)
            {
                ResultPulumiAction? actionResult = CreateResultForInputError(req);

                if (actionResult != null)
                {
                    results.Add(actionResult);
                    return Results.BadRequest(results);
                }

                // Inject GitHub credentials from configuration if available for all pulumi actions (generalization purpose)
                string githubToken = app.Configuration["GitHubToken"] ?? "";
                string githubOrganizationName = app.Configuration["GitHubOrganizationName"] ?? "";
                string gitlabToken = app.Configuration["GitLabToken"] ?? "";
                string gitlabBaseUrl = app.Configuration["GitLabBaseUrl"] ?? "";
                req.Parameters ??= new Dictionary<string, string>();

                if (HasRealConfigValue(githubToken))
                {
                    req.Parameters["githubToken"] = githubToken;
                }
                if (HasRealConfigValue(githubOrganizationName))
                {
                    req.Parameters["githubOrganizationName"] = githubOrganizationName;
                }
                if (HasRealConfigValue(gitlabToken))
                {
                    req.Parameters["gitlabToken"] = gitlabToken;
                }
                if (HasValidHttpUrl(gitlabBaseUrl))
                {
                    req.Parameters["gitlabBaseUrl"] = gitlabBaseUrl;
                }

                IResult result = await pulumiService.ExecuteAsync(req);

                actionResult = ProcessResult(result, req.Name, req.ResourceType, logger);

                if (actionResult.StatusCode >= 400)
                {
                    return Results.Json(actionResult, statusCode: actionResult.StatusCode);
                }
                results.Add(actionResult);
            }

            return Results.Ok(results);
        };

        // Azure Static Web Apps proxies backend requests via the fixed /api prefix.
        app.MapPost("/create-project", createProjectHandler);
        app.MapPost("/api/create-project", createProjectHandler);

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

    private static ResultPulumiAction? CreateResultForInputError(TemplateRequest request)
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
        Console.WriteLine($"Request received: Name={request.Name}, Type={request.ResourceType}");

        if (request.Name == null || string.IsNullOrWhiteSpace(request.Name))
        {
            return new ResultPulumiAction
            {
                Name = "NotSpecified",
                ResourceType = request.ResourceType ?? "NotSpecified",
                StatusCode = 400,
                Message = "Missing 'name' in request",
            };
        }
        if (request.ResourceType == null || string.IsNullOrWhiteSpace(request.ResourceType))
        {
            return new ResultPulumiAction
            {
                Name = request.Name,
                ResourceType = "NotSpecified",
                StatusCode = 400,
                Message = "Missing 'resourceType' parameter",
            };
        }

        // Add verification that the type exists in the pulumiPrograms folder
        return null;
    }

    private static ResultPulumiAction ProcessResult(
        IResult result,
        string name,
        string resourceType,
        ILogger log
    )
    {
        if (result is JsonHttpResult<Dictionary<string, object>> jsonResult)
        {
            log.LogInformation(
                "Resource created successfully: {Name} of {ResourceType}",
                name,
                resourceType
            );
            return new ResultPulumiAction
            {
                Name = name,
                ResourceType = resourceType,
                StatusCode = 200,
                Message = "Resource created successfully",
                Outputs = jsonResult.Value,
            };
        }
        else if (result is ProblemHttpResult problemResult)
        {
            string? errorData = problemResult.ProblemDetails.Detail;

            if (string.IsNullOrEmpty(errorData))
            {
                return new ResultPulumiAction
                {
                    Name = name,
                    ResourceType = resourceType,
                    StatusCode = 500,
                    Message = "Unknown error occurred, no details provided",
                };
            }

            const string PostErrorRegex = @"POST.*: (\d+) (.*?)\. \[(.*)\]";
            var match = Regex.Match(errorData, PostErrorRegex);
            if (match.Success)
            {
                var statusCodeStr = match.Groups[1].Value;
                var errorMessage = match.Groups[2].Value;
                var errorDetail = match.Groups[3].Value;

                // Try to parse the string status code into an integer
                if (int.TryParse(statusCodeStr, out var statusCodeInt))
                {
                    return new ResultPulumiAction
                    {
                        Name = name,
                        ResourceType = resourceType,
                        StatusCode = statusCodeInt,
                        Message = $"{errorMessage}. Details: {errorDetail}",
                    };
                }
            }

            const string MissingVariableRegex =
                @"Missing required configuration variable '.*?([a-zA-Z0-9]+)'";
            match = Regex.Match(errorData, MissingVariableRegex);
            if (match.Success)
            {
                var missingVar = match.Groups[1].Value;
                return new ResultPulumiAction
                {
                    Name = name,
                    ResourceType = resourceType,
                    StatusCode = 400,
                    Message = $"Missing required parameter '{missingVar}'",
                };
            }
            return new ResultPulumiAction
            {
                Name = name,
                ResourceType = resourceType,
                StatusCode = problemResult.ProblemDetails.Status ?? 500,
                Message =
                    problemResult.ProblemDetails.Detail
                    ?? "Unknown error occurred, no details provided",
            };
        }
        else
        {
            log.LogError("Unknown result type encountered in treatResult method.");
            log.LogError("Result type: {ResultType}", result.GetType().FullName);
            return new ResultPulumiAction
            {
                Name = name,
                ResourceType = resourceType,
                StatusCode = 500,
                Message = "Unknown result type",
            };
        }
    }
}
