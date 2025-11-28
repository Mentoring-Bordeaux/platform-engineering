using DotNetEnv;

var builder = WebApplication.CreateBuilder(args);
DotNetEnv.Env.Load();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("NuxtPolicy", policy =>
    {
        var nuxtAppUrl = Environment.GetEnvironmentVariable("NUXT_APP_URL") ?? "http://localhost:3001";
        policy.WithOrigins(nuxtAppUrl).AllowAnyHeader().AllowAnyMethod();
    });
});

// Register the generic Pulumi service
builder.Services.AddScoped<PulumiService>();

builder.Services.AddOpenApi();

var app = builder.Build();

app.UseCors("NuxtPolicy");

if (app.Environment.IsDevelopment()) app.MapOpenApi();

app.UseHttpsRedirection();


app.MapPost("/create-resource", async (TemplateRequest request, PulumiService service) =>
{
    if (string.IsNullOrEmpty(request.Name))
        return Results.BadRequest("The 'Name' field is required.");
    if (!request.Parameters.ContainsKey("githubToken"))
    {
        var githubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
        if (!string.IsNullOrEmpty(githubToken))
            request.Parameters["githubToken"] = githubToken;
    }

    return await service.ExecuteAsync(request);
});

app.Run();
