using SudokuNexus.Domain.Enums;
using SudokuNexus.Domain.Models;

namespace SudokuNexus.Engine.Grading.Strategies;

/// <summary>
/// Strategy Pattern contract for a human logical Sudoku deduction technique.
/// 
/// Architectural Note:
/// Each concrete strategy is registered in the Dependency Injection container as an
/// <see cref="IDeductionStrategy"/>. The <see cref="HumanLogicalGrader"/> receives
/// <c>IEnumerable&lt;IDeductionStrategy&gt;</c> via constructor injection, sorts them by
/// <see cref="DifficultyWeight"/>, and always attempts the simplest human technique first.
/// </summary>
public interface IDeductionStrategy
{
    TechniqueType Technique { get; }

    /// <summary>
    /// Sudoku Explainer (SE) inspired difficulty weight (e.g., 1.0 for Naked Single, 3.2 for X-Wing).
    /// </summary>
    double DifficultyWeight { get; }

    /// <summary>
    /// Inspects the current <paramref name="board"/> state and returns the first valid
    /// <see cref="SolveStep"/> found by this technique, or <c>null</c> if the technique
    /// cannot make progress on the current board.
    /// </summary>
    SolveStep? FindNextStep(Board board);
}
