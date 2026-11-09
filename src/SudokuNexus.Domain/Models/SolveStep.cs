using SudokuNexus.Domain.Enums;

namespace SudokuNexus.Domain.Models;

/// <summary>
/// Represents a single candidate digit removed from a specific cell during a logical deduction step.
/// </summary>
public readonly record struct CandidateElimination(int CellIndex, int Digit);

/// <summary>
/// Represents one human-understandable logical deduction step found by the grader engine.
/// A step either places a digit directly (e.g., Naked Single / Hidden Single) or eliminates
/// one or more pencil-mark candidates from peer cells (e.g., Pointing Pair, Naked Pair, X-Wing).
/// </summary>
public sealed record SolveStep(
    TechniqueType Technique,
    double DifficultyWeight,
    string Description,
    IReadOnlyList<int> PrimaryCells,
    int? PlacedCellIndex,
    int? PlacedDigit,
    IReadOnlyList<CandidateElimination> Eliminations
)
{
    public bool IsPlacement => PlacedCellIndex.HasValue && PlacedDigit.HasValue;

    /// <summary>
    /// Applies this logical step to the target <paramref name="board"/>.
    /// </summary>
    public void ApplyTo(Board board)
    {
        if (PlacedCellIndex.HasValue && PlacedDigit.HasValue)
        {
            board.PlaceDigit(PlacedCellIndex.Value, PlacedDigit.Value);
        }

        foreach (var elimination in Eliminations)
        {
            board.EliminateCandidate(elimination.CellIndex, elimination.Digit);
        }
    }
}

/// <summary>
/// Full report produced by the HumanLogicalGrader when analyzing a puzzle.
/// </summary>
public sealed record GradingReport(
    DifficultyRating Rating,
    TechniqueType MaxTechniqueUsed,
    double MaxStepWeight,
    double TotalWeightScore,
    bool SolvedEntirelyByLogic,
    string FinalBoardString,
    IReadOnlyList<SolveStep> Steps
);
