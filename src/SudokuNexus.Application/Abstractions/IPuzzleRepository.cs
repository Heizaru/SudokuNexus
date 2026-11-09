using SudokuNexus.Domain.Enums;

namespace SudokuNexus.Application.Abstractions;

/// <summary>
/// Persistence abstraction for storing and retrieving pre-generated, graded Sudoku puzzles.
/// Implemented in <c>SudokuNexus.Infrastructure</c> using EF Core 10 + SQLite.
/// </summary>
public interface IPuzzleRepository
{
    Task<StoredPuzzleDto?> GetRandomByDifficultyAsync(DifficultyRating difficulty, CancellationToken cancellationToken = default);
    Task<int> CountByDifficultyAsync(DifficultyRating difficulty, CancellationToken cancellationToken = default);
    Task AddPuzzleAsync(StoredPuzzleDto puzzle, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<DifficultyRating, int>> GetPoolStatisticsAsync(CancellationToken cancellationToken = default);
}

public sealed record StoredPuzzleDto(
    Guid Id,
    string PuzzleString,
    string SolutionString,
    DifficultyRating Difficulty,
    TechniqueType MaxTechnique,
    double TotalWeightScore,
    int GivenClueCount,
    DateTimeOffset CreatedAtUtc
);
