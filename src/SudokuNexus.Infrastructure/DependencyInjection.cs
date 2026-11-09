using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SudokuNexus.Application.Abstractions;
using SudokuNexus.Infrastructure.Background;
using SudokuNexus.Infrastructure.Persistence;

namespace SudokuNexus.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddSudokuInfrastructure(
            this IServiceCollection services,
            string connectionString = "Data Source=sudokunexus.db")
        {
            services.AddDbContext<SudokuDbContext>(options =>
                options.UseSqlite(connectionString));

            services.AddScoped<IPuzzleRepository, EfPuzzleRepository>();
            services.AddSingleton<IPuzzleGenerationQueue, ChannelPuzzleGenerationQueue>();
            services.AddHostedService<PuzzlePoolBackgroundWorker>();

            return services;
        }
    }
}
