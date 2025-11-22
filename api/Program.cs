using Pulumi.Automation;
using System.Text.RegularExpressions;

//////////////////////////////////////////////// Configure the API  ////////////////////////////////////////////////

// Create a builder for the web application

var builder = WebApplication.CreateBuilder(args);


// Configure CORS to allow requests from Nuxt 4 development server
builder.Services.AddCors(options =>
{
    options.AddPolicy("NuxtPolicy",
        policy =>
        {
            var nuxtAppUrl = builder.Configuration["NuxtAppUrl"] ?? "http://localhost:3001";
            policy.WithOrigins(nuxtAppUrl)
                  .AllowAnyHeader()
                  .AllowAnyMethod();

        });
});

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Log the configured CORS policy
var nuxtAppUrl = builder.Configuration["NuxtAppUrl"] ?? "http://localhost:3001";
app.Logger.LogInformation("Configuring CORS to allow requests from: {NuxtAppUrl}", nuxtAppUrl);

// Use the configured CORS policy
app.UseCors("NuxtPolicy");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

////////////////////////////////////////////// Pulumi Automation API Logic  ////////////////////////////////////////////////

// Method to create a GitHub repository using Pulumi Automation API
async Task<IResult> CreateGitHubRepository(CreateRepoRequest request, IConfiguration config, ILogger logger)
{
    var stackName = "github-repo-stack" + request.RepoName.Replace(" ", "-").ToLowerInvariant();
    var stack = null as WorkspaceStack;

    var executingDir = Directory.GetCurrentDirectory();
    var workingDir = Path.Combine(executingDir, "pulumiPrograms", "github");
    try
    {
        var stackArgs = new LocalProgramArgs(stackName, workingDir);
        stack = await LocalWorkspace.CreateOrSelectStackAsync(stackArgs);

        // Retrieve GitHub token from configuration (user secrets or environment variables)
        var githubToken = config["GitHubToken"];
        if (string.IsNullOrEmpty(githubToken))
        {
            return Results.Problem("GitHub token is not provided in the configuration.", statusCode: 401);
        }
        await stack.SetConfigAsync("githubToken", new ConfigValue(githubToken));
        await stack.SetConfigAsync("repoName", new ConfigValue(request.RepoName));
        if (!string.IsNullOrEmpty(request.Description))
        {
            await stack.SetConfigAsync("description", new ConfigValue(request.Description));
        }
        await stack.SetConfigAsync("isPrivate", new ConfigValue(request.Private.ToString().ToLowerInvariant()));

        // Retrieve organization name from configuration (user secrets or environment variables)
        var orgName = config["OrganizationName"];
        if (!string.IsNullOrEmpty(orgName))
        {
            await stack.SetConfigAsync("orgName", new ConfigValue(orgName));
        }

        // Run the Pulumi program
        var result = await stack.UpAsync(new UpOptions
        {
            OnStandardOutput = (output) => logger.LogInformation("Pulumi: {Output}", output),
            OnStandardError = (error) => logger.LogError("Pulumi ERROR: {Error}", error)
        });

        // Retrieve outputs
        var outputs = result.Outputs;
        var repoNameOutput = outputs["repoNameOutput"].Value.ToString();
        var repoUrl = outputs["repoUrl"].Value.ToString();

        // Return the created repository information
        return Results.Ok(new
        {
            RepoName = repoNameOutput,
            RepoUrl = repoUrl
        });

    }
    catch (Exception ex)
    {
        // Regex pattern to capture Status Code (Group 1), Error Message (Group 2), and Detail (Group 3)
        const string githubErrorRegex = @"POST.*: (\d+) (.*?)\. \[(.*)\]";

        var match = Regex.Match(ex.Message, githubErrorRegex);
        if (match.Success)
        {
            var statusCodeStr = match.Groups[1].Value;
            var errorMessage = match.Groups[2].Value;
            var errorDetail = match.Groups[3].Value;

            // Try to parse the string status code into an integer
            if (int.TryParse(statusCodeStr, out var statusCodeInt))
            {
                logger.LogError("GitHub API error occurred: {ErrorMessage} (Status code: {StatusCode})", errorMessage, statusCodeInt);

                // Return Results.Problem with the explicit status code
                // This will set the actual HTTP response status to 422 for Repository creation or 403 for Token permission issues
                return Results.Problem(
                    detail: $"GitHub API error occurred: {errorMessage}. Detail: {errorDetail}",
                    statusCode: statusCodeInt,
                    title: "Repository Creation Failed",
                    type: "github-error"
                );
            }
            // Fallback for parsing failure
            return Results.Problem($"An internal error occurred while processing the GitHub API response: {ex.Message}");
        }

        logger.LogError(ex, "General exception occurred during repository creation");
        // General Pulumi or unparsed exception
        return Results.Problem($"An error occurred: {ex.Message}");
    }
    finally
    {
        if (stack != null)
        {
            // Delete the stack's resources
            await stack.DestroyAsync();

            // Delete the stack's workspace
            await stack.Workspace.RemoveStackAsync(stackName);

            //Delete the stack's yaml file to avoid conflicts on next creation
            var stackYamlPath = Path.Combine(workingDir, $"Pulumi.{stackName}.yaml");
            if (File.Exists(stackYamlPath))
            {
                File.Delete(stackYamlPath);
            }
        }
    }

}


async Task<IResult> CreateStaticWebapp(StaticWebSiteRequest request, ILogger logger)
{

    var stackName = "storage-staticweb-stack" + Regex.Replace(request.Name.ToLower(), @"[^a-z0-9\-]", "-");

    var executingDir = Directory.GetCurrentDirectory();
    var workingDir = Path.Combine(executingDir, "pulumiPrograms", "staticWebApp");

    try
    {
        var stackArgs = new LocalProgramArgs(stackName, workingDir);
        var stack = await LocalWorkspace.CreateOrSelectStackAsync(stackArgs);

        
        await stack.SetConfigAsync("Name", new ConfigValue(request.Name));

        var result = await stack.UpAsync(new UpOptions
        {
            OnStandardOutput = (output) => logger.LogInformation("Pulumi: {Output}", output),
            OnStandardError = (error) => logger.LogError("Pulumi ERROR: {Error}", error)
        });

        var outputs = result.Outputs.ToDictionary(
            kv => kv.Key, 
            kv => kv.Value.Value
        );
        return TypedResults.Json(outputs, statusCode: 200);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error occurred while creating static web app");
        return TypedResults.Json(
            new { message = "Erreur" },
            statusCode: 500
        );
    }
}


//////////////////////////////////////////////////// Define API Endpoints ////////////////////////////////////////////////


// Define the request model for creating a repository
app.MapPost("/create-repo", async (CreateRepoRequest request, ILogger<Program> logger) =>
{
    if (string.IsNullOrEmpty(request.RepoName))
    {
        logger.LogWarning("Repository creation attempted with empty RepoName");
        return Results.BadRequest("The 'RepoName' field is required.");
    }

    logger.LogInformation("Creating GitHub repository: {RepoName}", request.RepoName);

    // Call the method to create the repository
    var result = await CreateGitHubRepository(
        request,
        app.Configuration,
        logger
    );
    return result;

});

app.MapPost("/create-staticweb", async (StaticWebSiteRequest request, ILogger<Program> logger) =>
{
    logger.LogInformation("Creating static web app: {Name}", request.Name);
    return await CreateStaticWebapp(request, logger);
});

app.Run();
