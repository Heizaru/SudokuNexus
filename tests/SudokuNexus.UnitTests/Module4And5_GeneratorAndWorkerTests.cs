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

/// <summary>
/// Run these tests while working on Modules 4 &amp; 5:
/// <c>dotnet test --filter "Module=4|Module=5"</c>
/// </summary>
public sealed class Module4And5_GeneratorAndWorkerTests
{
    [Fact]
    [Trait("Module", "4")]
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

        // Verify 180-degree rotational symmetry of given clues
        for (int i = 0; i < 81; i++)
        {
            bool hasClueA = puzzleBoard.GetValue(i) != 0;
            bool hasClueB = puzzleBoard.GetValue(80 - i) != 0;
            Assert.Equal(hasClueA, hasClueB);
        }
    }

    [Fact]
    [Trait("Module", "5")]
    public async Task PuzzlePoolBackgroundWorker_ProcessGenerationRequest_PersistsToScopedRepository()
    {
        var services = new ServiceCollection();
        services.AddSudokuApplication();
        services.AddDbContext<SudokuDbContext>(o => o.UseSqlite("DataSource=file:memdb_worker?mode=memory&cache=shared"));
        services.AddScoped<IPuzzleRepository, EfPuzzleRepository>();
        services.AddSingleton<IPuzzleGenerationQueue, ChannelPuzzleGenerationQueue>();

        await using ServiceProvider provider = services.BuildServiceProvider();

        // Keep shared in-memory connection open for test lifetime
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
}
