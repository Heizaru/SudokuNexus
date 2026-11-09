using Microsoft.Extensions.DependencyInjection;
using SudokuNexus.Application.Services;
using SudokuNexus.Engine;

namespace SudokuNexus.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddSudokuApplication(this IServiceCollection services)
    {
        services.AddSudokuEngine();
        services.AddScoped<PuzzleApplicationService>();
        return services;
    }
}
