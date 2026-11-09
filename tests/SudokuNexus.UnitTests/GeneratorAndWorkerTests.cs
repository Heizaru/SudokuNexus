using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SudokuNexus.Application;
using SudokuNexus.Application.Abstractions;
using SudokuNexus.Domain.Enums;
using SudokuNexus.Domain.Models;
using SudokuNexus.Engine.ExactCover;
using SudokuNexus.Engine.Generation;
using SudokuNexus.Engine.Grading;
using SudokuNexus.Engine.Grading.Strategies;
using SudokuNexus.Infrastructure.Background;
using SudokuNexus.Infrastructure.Persistence;
using Xunit;

namespace SudokuNexus.UnitTests;

public sealed class GeneratorAndWorkerTests
{
    [Fact]
    public void PuzzleGenerator_GeneratesSymmetricPuzzleWithUniqueSolution()
    {
        var dlx = new DlxSolver();
        var grader = new HumanLogicalGrader(
        [
            new NakedSingleStrategy(),
            new HiddenSingleStrategy(),
            new PointingPairStrategy(),
            new NakedPairStrategy(),
            new XWingStrategy()
        ]);
        var generator = new PuzzleGenerator(dlx, grader);

        GeneratedPuzzle generated = generator.Generate(DifficultyRating.Easy, new Random(42));

        Board puzzleBoard = Board.TryParse(generated.PuzzleString).Value!;
        Assert.True(puzzleBoard.IsValid());
        Assert.Equal(1, dlx.CountSolutions(puzzleBoard, limit: 2));

        for (int i = 0; i < 81; i++)
        {
            bool hasClueA = puzzleBoard.GetValue(i) != 0;
            bool hasClueB = puzzleBoard.GetValue(80 - i) != 0;
            Assert.Equal(hasClueA, hasClueB);
        }
    }

    [Fact]
    public async Task PuzzlePoolBackgroundWorker_ProcessGenerationRequest_PersistsToScopedRepository()
    {
        var services = new ServiceCollection();
        services.AddSudokuApplication();
        services.AddDbContext<SudokuDbContext>(o => o.UseSqlite("DataSource=file:memdb_worker?mode=memory&cache=shared"));
        services.AddScoped<IPuzzleRepository, EfPuzzleRepository>();
        services.AddSingleton<IPuzzleGenerationQueue, ChannelPuzzleGenerationQueue>();

        await using ServiceProvider provider = services.BuildServiceProvider();

        await using AsyncServiceScope initScope = provider.CreateAsyncScope();
        var db = initScope.ServiceProvider.GetRequiredService<SudokuDbContext>();
        await db.Database.OpenConnectionAsync();
        await db.Database.EnsureCreatedAsync();

        var worker = new PuzzlePoolBackgroundWorker(
            provider.GetRequiredService<IPuzzleGenerationQueue>(),
            provider.GetRequiredService<PuzzleGenerator>(),
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<PuzzlePoolBackgroundWorker>.Instance);

        bool added = await worker.ProcessGenerationRequestAsync(DifficultyRating.Easy);
        Assert.True(added);

        var repo = initScope.ServiceProvider.GetRequiredService<IPuzzleRepository>();
        int count = await repo.CountByDifficultyAsync(DifficultyRating.Easy);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task PuzzlePoolBackgroundWorker_ProcessGenerationRequest_ReturnsFalseWhenPoolIsFull()
    {
        var services = new ServiceCollection();
        services.AddSudokuApplication();
        services.AddDbContext<SudokuDbContext>(o => o.UseSqlite("DataSource=file:memdb_worker_full?mode=memory&cache=shared"));
        services.AddScoped<IPuzzleRepository, EfPuzzleRepository>();
        services.AddSingleton<IPuzzleGenerationQueue, ChannelPuzzleGenerationQueue>();

        await using ServiceProvider provider = services.BuildServiceProvider();

        await using AsyncServiceScope initScope = provider.CreateAsyncScope();
        var db = initScope.ServiceProvider.GetRequiredService<SudokuDbContext>();
        await db.Database.OpenConnectionAsync();
        await db.Database.EnsureCreatedAsync();

        var worker = new PuzzlePoolBackgroundWorker(
            provider.GetRequiredService<IPuzzleGenerationQueue>(),
            provider.GetRequiredService<PuzzleGenerator>(),
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<PuzzlePoolBackgroundWorker>.Instance);

        for (int i = 0; i < PuzzlePoolBackgroundWorker.TargetPoolSizePerDifficulty; i++)
        {
            Assert.True(await worker.ProcessGenerationRequestAsync(DifficultyRating.Easy));
        }

        bool addedWhenFull = await worker.ProcessGenerationRequestAsync(DifficultyRating.Easy);
        Assert.False(addedWhenFull);

        var repo = initScope.ServiceProvider.GetRequiredService<IPuzzleRepository>();
        int count = await repo.CountByDifficultyAsync(DifficultyRating.Easy);
        Assert.Equal(PuzzlePoolBackgroundWorker.TargetPoolSizePerDifficulty, count);
    }
}
