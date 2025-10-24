var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

// Configure CORS to allow requests from Nuxt 3 development server
builder.Services.AddCors(options =>
{
    options.AddPolicy("NuxtPolicy",
        policy =>
        {
            // Retrieve the Nuxt app URL from environment variables and allow it
            var nuxtAppUrl = builder.Configuration["NUXT_APP_URL"] ?? "http://localhost:3000"; // Can't be null
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

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
