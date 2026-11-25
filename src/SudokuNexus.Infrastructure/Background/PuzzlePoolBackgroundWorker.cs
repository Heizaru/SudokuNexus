using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SudokuNexus.Application.Abstractions;
using SudokuNexus.Domain.Enums;
using SudokuNexus.Engine.Generation;

namespace SudokuNexus.Infrastructure.Background;

/// <summary>
/// Background worker that consumes puzzle generation requests from <see cref="IPuzzleGenerationQueue"/>
/// and replenishes the SQLite puzzle pool per difficulty rating.
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
    /// Processes a single generation request using a scoped <see cref="IPuzzleRepository"/>.
    /// </summary>
    public async Task<bool> ProcessGenerationRequestAsync(
        DifficultyRating targetDifficulty,
        CancellationToken cancellationToken = default)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IPuzzleRepository>();

        int currentCount = await repo.CountByDifficultyAsync(targetDifficulty, cancellationToken);
        if (currentCount >= TargetPoolSizePerDifficulty)
        {
            return false;
        }

        GeneratedPuzzle generated = generator.Generate(targetDifficulty);
        var dto = new StoredPuzzleDto(
            Id: Guid.NewGuid(),
            PuzzleString: generated.PuzzleString,
            SolutionString: generated.SolutionString,
            Difficulty: targetDifficulty,
            MaxTechnique: generated.GradingReport.MaxTechniqueUsed,
            TotalWeightScore: generated.GradingReport.TotalWeightScore,
            GivenClueCount: generated.GivenClueCount,
            CreatedAtUtc: DateTimeOffset.UtcNow);

        await repo.AddPuzzleAsync(dto, cancellationToken);
        return true;
    }
}
