using SudokuNexus.Api.Endpoints;
using SudokuNexus.Api.Middleware;
using SudokuNexus.Application;
using SudokuNexus.Infrastructure;
using SudokuNexus.Infrastructure.Persistence;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddSudokuApplication();
builder.Services.AddSudokuInfrastructure(
    builder.Configuration.GetConnectionString("SudokuDb") ?? "Data Source=sudokunexus.db");

WebApplication app = builder.Build();

// Ensure SQLite database schema is created on startup
using (IServiceScope scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SudokuDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.UseExceptionHandler();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapOpenApi();
app.MapPuzzleEndpoints();

app.Run();
