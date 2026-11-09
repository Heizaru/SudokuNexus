using SudokuNexus.Domain.Enums;
using SudokuNexus.Domain.Models;
using SudokuNexus.Domain.Primitives;

namespace SudokuNexus.Engine.Grading.Strategies;

/// <summary>
/// If an empty cell has only 1 remaining candidate bit set in its <see cref="CandidateMask"/>,
/// that digit must be placed in that cell.
/// </summary>
public sealed class NakedSingleStrategy : IDeductionStrategy
{
    public TechniqueType Technique => TechniqueType.NakedSingle;
    public double DifficultyWeight => 1.0;

    public SolveStep? FindNextStep(Board board)
    {
        for (var cellIndex = 0; cellIndex < SudokuTopology.TotalCells; cellIndex++)
        {
            if (board.GetValue(cellIndex) != 0)
            {
                continue;
            }

            CandidateMask mask = board.GetCandidates(cellIndex);
            if (mask.Count == 1)
            {
                int digit = mask.GetSingleDigit();
                var cellCoordinate = new CellCoordinate(cellIndex);
                var description = $"Naked Single: {cellCoordinate} can only contain {digit}";
                return new SolveStep(Technique, DifficultyWeight, description, [cellIndex], cellIndex, digit, []);
            }
        }

        return null;
    }
}
