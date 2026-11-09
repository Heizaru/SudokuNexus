using Microsoft.EntityFrameworkCore;
using SudokuNexus.Application.Abstractions;
using SudokuNexus.Domain.Enums;
using SudokuNexus.Domain.Models;
using SudokuNexus.Domain.Primitives;
using SudokuNexus.Engine.ExactCover;
using SudokuNexus.Infrastructure.Background;
using SudokuNexus.Infrastructure.Persistence;
using Xunit;

namespace SudokuNexus.UnitTests;

public sealed class InfrastructureAndTopologyTests
{
    [Fact]
    public void SudokuTopology_EachCellHasExactly20DistinctPeers()
    {
        for (int cell = 0; cell < SudokuTopology.TotalCells; cell++)
        {
            int[] peers = SudokuTopology.Peers[cell];
            Assert.Equal(20, peers.Length);
            Assert.Equal(20, peers.Distinct().Count());
            Assert.DoesNotContain(cell, peers);
        }
    }

    [Fact]
    public void CellCoordinate_ComputesRowColBoxAndNotationCorrectly()
    {
        var topLeft = new CellCoordinate(0);
        Assert.Equal(0, topLeft.Row);
        Assert.Equal(0, topLeft.Column);
        Assert.Equal(0, topLeft.Box);
        Assert.Equal("R1C1", topLeft.ToString());

        CellCoordinate center = CellCoordinate.FromRowCol(4, 4);
        Assert.Equal(40, center.Index);
        Assert.Equal(4, center.Box);
        Assert.Equal("R5C5", center.ToString());
    }

    [Fact]
    public void DlxToroidalMatrix_Has324ColumnsAnd9NodesPerColumn()
    {
        Span<short> left = stackalloc short[DlxSolver.TotalNodes];
        Span<short> right = stackalloc short[DlxSolver.TotalNodes];
        Span<short> up = stackalloc short[DlxSolver.TotalNodes];
        Span<short> down = stackalloc short[DlxSolver.TotalNodes];
        Span<short> size = stackalloc short[DlxSolver.TotalColumns + 1];

        DlxSolver.CopyTemplateTo(left, right, up, down, size);

        int colCount = 0;
        for (short col = right[DlxSolver.HeaderNode]; col != DlxSolver.HeaderNode; col = right[col])
        {
            colCount++;
            Assert.Equal(colCount, col);
            Assert.Equal(9, size[col]);
        }

        Assert.Equal(324, colCount);
    }

    [Fact]
    public async Task EfPuzzleRepository_CanPersistAndQueryPuzzlesInMemorySqlite()
    {
        DbContextOptions<SudokuDbContext> options = new DbContextOptionsBuilder<SudokuDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        await using var context = new SudokuDbContext(options);
        await context.Database.OpenConnectionAsync();
        await context.Database.EnsureCreatedAsync();

        var repo = new EfPuzzleRepository(context);
        var dto = new StoredPuzzleDto(
            Guid.NewGuid(),
            new string('.', 81),
            new string('1', 81),
            DifficultyRating.Hard,
            TechniqueType.NakedPair,
            42.5,
            28,
            DateTimeOffset.UtcNow);

        await repo.AddPuzzleAsync(dto);

        int count = await repo.CountByDifficultyAsync(DifficultyRating.Hard);
        Assert.Equal(1, count);

        StoredPuzzleDto? fetched = await repo.GetRandomByDifficultyAsync(DifficultyRating.Hard);
        Assert.NotNull(fetched);
        Assert.Equal(dto.Id, fetched.Id);
    }

    [Fact]
    public async Task ChannelPuzzleGenerationQueue_EnqueuesAndReadsInOrder()
    {
        var queue = new ChannelPuzzleGenerationQueue(capacity: 4);
        await queue.EnqueueAsync(DifficultyRating.Easy);
        await queue.EnqueueAsync(DifficultyRating.Expert);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var results = new List<DifficultyRating>();

        await foreach (DifficultyRating item in queue.ReadAllAsync(cts.Token))
        {
            results.Add(item);
            if (results.Count == 2) break;
        }

        Assert.Equal([DifficultyRating.Easy, DifficultyRating.Expert], results);
    }
}
