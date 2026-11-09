using SudokuNexus.Domain.Enums;

namespace SudokuNexus.Infrastructure.Persistence;

public sealed class PuzzleEntity
{
    public Guid Id { get; set; }
    public string PuzzleString { get; set; } = string.Empty;
    public string SolutionString { get; set; } = string.Empty;
    public DifficultyRating Difficulty { get; set; }
    public TechniqueType MaxTechnique { get; set; }
    public double TotalWeightScore { get; set; }
    public int GivenClueCount { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
