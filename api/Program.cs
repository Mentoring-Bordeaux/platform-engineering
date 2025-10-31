using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Reflection;

using Microsoft.AspNetCore.Mvc;
using Pulumi.Automation;
using System.Text.RegularExpressions;


//////////////////////////////////////////////// Configure the API  ////////////////////////////////////////////////

// Create a builder for the web application

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

// Configure CORS to allow requests from Nuxt 4 development server
builder.Services.AddCors(options =>
{
    options.AddPolicy("NuxtPolicy",
        policy =>
        {
            // Retrieve the Nuxt app URL from environment variables and allow it
            var nuxtAppUrl = builder.Configuration["NUXT_APP_URL"] ?? "http://localhost:3000"; // Can't be null  (TODO: throw error if null?)
            policy.WithOrigins(nuxtAppUrl)
                  .AllowAnyHeader()
                  .AllowAnyMethod();

        });
});

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Use the configured CORS policy
app.UseCors("NuxtPolicy");


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();


////////////////////////////////////////////// Pulumi Automation API Logic  ////////////////////////////////////////////////



async Task<IResult> CreateGitHubRepository(CreateRepoRequest request)
{

    var stackName = "github-repo-stack";
    var stack = null as WorkspaceStack;

    var executingDir = Directory.GetCurrentDirectory();
    var workingDir = Path.Combine(executingDir, "pulumiPrograms", "github");
    try
    {


        var stackArgs = new LocalProgramArgs(stackName, workingDir);
        stack = await LocalWorkspace.CreateOrSelectStackAsync(stackArgs);

        await stack.SetConfigAsync("githubToken", new ConfigValue(request.GithubToken));
        await stack.SetConfigAsync("repoName", new ConfigValue(request.RepoName));
        if (request.Description != null && request.Description != "")
        {
            await stack.SetConfigAsync("description", new ConfigValue(request.Description));
        }
        await stack.SetConfigAsync("isPrivate", new ConfigValue(request.Private.ToString().ToLowerInvariant()));
        if (request.OrgName != null && request.OrgName != "")
        {
            await stack.SetConfigAsync("orgName", new ConfigValue(request.OrgName));
        }

        // Run the Pulumi program
        var result = await stack.UpAsync(new UpOptions
        {
            OnStandardOutput = (output) => Console.WriteLine(output),
            OnStandardError = (error) => Console.WriteLine($"ERROR: {error}")


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
                Console.WriteLine($"GitHub API error occurred: {errorMessage} (Status code: {statusCodeInt})");

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

        Console.WriteLine("Outputting general exception details.");
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


//////////////////////////////////////////////////// Define API Endpoints ////////////////////////////////////////////////


// Define the request model for creating a repository
app.MapPost("/create-repo", async (CreateRepoRequest request) =>
{
    if (string.IsNullOrEmpty(request.GithubToken) || string.IsNullOrEmpty(request.RepoName))
    {
        return Results.BadRequest("The 'GithubToken' and 'RepoName' fields are required.");
    }

    // Call the method to create the repository
    var result = await CreateGitHubRepository(
        request
    );
    return result;

});

app.Run();






