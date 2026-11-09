using SudokuNexus.Domain.Enums;
using SudokuNexus.Domain.Models;
using SudokuNexus.Domain.Primitives;

namespace SudokuNexus.Engine.Grading.Strategies;

/// <summary>
/// Hidden Single Strategy (Weight: 1.5).
/// Places a digit when it appears as a candidate in only one empty cell within a row, column, or 3x3 box.
/// </summary>
public sealed class HiddenSingleStrategy : IDeductionStrategy
{
    public TechniqueType Technique => TechniqueType.HiddenSingle;
    public double DifficultyWeight => 1.5;

    private static readonly HouseType[] HouseOrder = [HouseType.Box, HouseType.Row, HouseType.Column];

    public SolveStep? FindNextStep(Board board)
    {
        foreach (HouseType houseType in HouseOrder)
        {
            for (int houseIndex = 0; houseIndex < SudokuTopology.GridSize; houseIndex++)
            {
                ReadOnlySpan<int> houseCells = SudokuTopology.GetHouseCells(houseType, houseIndex);

                for (int digit = 1; digit <= 9; digit++)
                {
                    int count = 0;
                    int targetCell = -1;

                    foreach (int cellIndex in houseCells)
                    {
                        if (board.GetValue(cellIndex) == 0 && board.GetCandidates(cellIndex).HasCandidate(digit))
                        {
                            count++;
                            targetCell = cellIndex;
                            if (count > 1)
                            {
                                break;
                            }
                        }
                    }

                    if (count == 1)
                    {
                        string description =
                            $"Hidden Single: Digit {digit} can only go in {new CellCoordinate(targetCell)} within {houseType} {houseIndex + 1}";
                        return new SolveStep(
                            Technique,
                            DifficultyWeight,
                            description,
                            PrimaryCells: [targetCell],
                            PlacedCellIndex: targetCell,
                            PlacedDigit: digit,
                            Eliminations: []);
                    }
                }
            }
        }

        return null;
    }

}
