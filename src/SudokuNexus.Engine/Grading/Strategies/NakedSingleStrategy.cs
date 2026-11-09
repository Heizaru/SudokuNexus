using SudokuNexus.Domain.Enums;
using SudokuNexus.Domain.Models;
using SudokuNexus.Domain.Primitives;

namespace SudokuNexus.Engine.Grading.Strategies;

/// <summary>
/// MODULE 3.1: Naked Single Strategy (Weight: 1.0).
/// 
/// Rule:
/// If an empty cell has only 1 remaining candidate bit set in its <see cref="CandidateMask"/>,
/// that digit MUST be placed in that cell.
/// </summary>
public sealed class NakedSingleStrategy : IDeductionStrategy
{
    public TechniqueType Technique => TechniqueType.NakedSingle;
    public double DifficultyWeight => 1.0;

    public SolveStep? FindNextStep(Board board)
    {
        // TODO [Module 3.1]:
        // Scan all 81 cells (`cellIndex` from 0 to 80):
        // 1. Skip cells that already have a value (`board.GetValue(cellIndex) != 0`).
        // 2. Get the `CandidateMask mask = board.GetCandidates(cellIndex)`.
        // 3. If `mask.Count == 1`:
        //    - Extract the single digit using `int digit = mask.GetSingleDigit()`.
        //    - Format a clear explanation using `new CellCoordinate(cellIndex)`
        //      (e.g., $"Naked Single: {coord} can only contain {digit}.").
        //    - Return a `new SolveStep(Technique, DifficultyWeight, description, [cellIndex], cellIndex, digit, [])`.
        // 4. If no cell has a single candidate, return `null`.
        throw new NotImplementedException("TODO [Module 3.1]: Implement NakedSingleStrategy.FindNextStep.");
    }
}
