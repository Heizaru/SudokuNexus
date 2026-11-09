using SudokuNexus.Domain.Enums;
using SudokuNexus.Domain.Models;
using SudokuNexus.Domain.Primitives;

namespace SudokuNexus.Engine.Grading.Strategies;

/// <summary>
/// MODULE 3.2: Hidden Single Strategy (Weight: 1.5).
/// 
/// Rule:
/// Within any House (3x3 Box, Row, or Column), if a candidate digit d (1..9) appears
/// in the <see cref="CandidateMask"/> of ONLY ONE empty cell in that House, then d MUST
/// be placed in that cell (even if that cell has other pencil-mark candidates).
/// </summary>
public sealed class HiddenSingleStrategy : IDeductionStrategy
{
    public TechniqueType Technique => TechniqueType.HiddenSingle;
    public double DifficultyWeight => 1.5;

    private static readonly HouseType[] HouseOrder = [HouseType.Box, HouseType.Row, HouseType.Column];

    public SolveStep? FindNextStep(Board board)
    {
        // TODO [Module 3.2]:
        // Iterate through each `HouseType` in `HouseOrder` (Boxes are easiest for humans to spot first, then Rows, then Columns):
        //   For each `houseIndex` from 0 to 8:
        //     Get the 9 cell indices of the house via `ReadOnlySpan<int> cells = SudokuTopology.GetHouseCells(houseType, houseIndex)`.
        //     For each `digit` from 1 to 9:
        //       Count how many empty cells in `cells` have `board.GetCandidates(cell).HasCandidate(digit)`.
        //       If `count == 1`:
        //         We found a Hidden Single at `targetCell`!
        //         Return `new SolveStep(Technique, DifficultyWeight, description, [targetCell], targetCell, digit, [])`
        //         where `description` is e.g. $"Hidden Single: Digit {digit} can only go in {new CellCoordinate(targetCell)} within {houseType} {houseIndex + 1}."
        // Return `null` if no Hidden Single exists.
        throw new NotImplementedException("TODO [Module 3.2]: Implement HiddenSingleStrategy.FindNextStep.");
    }
}
