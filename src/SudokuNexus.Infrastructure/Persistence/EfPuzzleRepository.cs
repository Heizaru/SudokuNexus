using Microsoft.EntityFrameworkCore;
using SudokuNexus.Application.Abstractions;
using SudokuNexus.Domain.Enums;

namespace SudokuNexus.Infrastructure.Persistence;

public sealed class EfPuzzleRepository(SudokuDbContext dbContext) : IPuzzleRepository
{
    public async Task<StoredPuzzleDto?> GetRandomByDifficultyAsync(
        DifficultyRating difficulty,
        CancellationToken cancellationToken = default)
    {
        List<PuzzleEntity> candidates = await dbContext.Puzzles
            .AsNoTracking()
            .Where(p => p.Difficulty == difficulty)
            .ToListAsync(cancellationToken);

        if (candidates.Count == 0)
        {
            return null;
        }

        PuzzleEntity chosen = candidates[Random.Shared.Next(candidates.Count)];
        return new StoredPuzzleDto(
            chosen.Id,
            chosen.PuzzleString,
            chosen.SolutionString,
            chosen.Difficulty,
            chosen.MaxTechnique,
            chosen.TotalWeightScore,
            chosen.GivenClueCount,
            chosen.CreatedAtUtc);
    }

    public Task<int> CountByDifficultyAsync(
        DifficultyRating difficulty,
        CancellationToken cancellationToken = default) =>
        dbContext.Puzzles.CountAsync(p => p.Difficulty == difficulty, cancellationToken);

    public async Task AddPuzzleAsync(
        StoredPuzzleDto puzzle,
        CancellationToken cancellationToken = default)
    {
        var entity = new PuzzleEntity
        {
            Id = puzzle.Id,
            PuzzleString = puzzle.PuzzleString,
            SolutionString = puzzle.SolutionString,
            Difficulty = puzzle.Difficulty,
            MaxTechnique = puzzle.MaxTechnique,
            TotalWeightScore = puzzle.TotalWeightScore,
            GivenClueCount = puzzle.GivenClueCount,
            CreatedAtUtc = puzzle.CreatedAtUtc
        };

        dbContext.Puzzles.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<DifficultyRating, int>> GetPoolStatisticsAsync(
        CancellationToken cancellationToken = default)
    {
        var counts = await dbContext.Puzzles
            .AsNoTracking()
            .GroupBy(p => p.Difficulty)
            .Select(g => new { Difficulty = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        Dictionary<DifficultyRating, int> result = Enum.GetValues<DifficultyRating>().ToDictionary(d => d, _ => 0);
        foreach (var item in counts)
        {
            result[item.Difficulty] = item.Count;
        }
        return result;
    }
}
