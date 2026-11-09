using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SudokuNexus.Application.Abstractions;
using SudokuNexus.Domain.Enums;
using SudokuNexus.Engine.Generation;

namespace SudokuNexus.Infrastructure.Background;

/// <summary>
/// MODULE 5.1: Enterprise Background Worker using <see cref="BackgroundService"/> and <see cref="IAsyncEnumerable{T}"/>.
/// 
/// Architectural Note:
/// In ASP.NET Core, a <see cref="BackgroundService"/> is registered as a Singleton, whereas EF Core's
/// <c>DbContext</c> and <see cref="IPuzzleRepository"/> are Scoped!
/// Therefore, a background worker must inject <see cref="IServiceScopeFactory"/> and create a fresh
/// <see cref="IServiceScope"/> for each unit of work.
/// </summary>
public sealed class PuzzlePoolBackgroundWorker(
    IPuzzleGenerationQueue queue,
    PuzzleGenerator generator,
    IServiceScopeFactory scopeFactory,
    ILogger<PuzzlePoolBackgroundWorker> logger) : BackgroundService
{
    public const int TargetPoolSizePerDifficulty = 5;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("PuzzlePoolBackgroundWorker started.");

        try
        {
            await foreach (DifficultyRating requestedDifficulty in queue.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await ProcessGenerationRequestAsync(requestedDifficulty, stoppingToken);
                }
                catch (NotImplementedException)
                {
                    // Expected before Module 4 & 5 are completed by the developer.
                    logger.LogDebug("Background generation skipped: Module 4/5 not yet implemented.");
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "Error while generating background puzzle for {Difficulty}.", requestedDifficulty);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("PuzzlePoolBackgroundWorker stopping gracefully.");
        }
    }

    /// <summary>
    /// MODULE 5.1: Processes a single generation request using a scoped <see cref="IPuzzleRepository"/>.
    /// </summary>
    public async Task<bool> ProcessGenerationRequestAsync(
        DifficultyRating targetDifficulty,
        CancellationToken cancellationToken = default)
    {
        _ = generator;
        _ = scopeFactory;
        _ = targetDifficulty;
        _ = cancellationToken;

        // TODO [Module 5.1]:
        // 1. Create an async DI scope: `await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();`
        // 2. Resolve `IPuzzleRepository repo = scope.ServiceProvider.GetRequiredService<IPuzzleRepository>();`
        // 3. Check current pool count: `int currentCount = await repo.CountByDifficultyAsync(targetDifficulty, cancellationToken);`
        //    If `currentCount >= TargetPoolSizePerDifficulty`, return `false` (pool is already full!).
        // 4. Generate a new puzzle: `GeneratedPuzzle generated = generator.Generate(targetDifficulty);`
        // 5. Persist it via `await repo.AddPuzzleAsync(new StoredPuzzleDto(...), cancellationToken);`
        // 6. Return `true`.
        throw new NotImplementedException("TODO [Module 5.1]: Implement PuzzlePoolBackgroundWorker.ProcessGenerationRequestAsync using IServiceScopeFactory.");
    }
}
