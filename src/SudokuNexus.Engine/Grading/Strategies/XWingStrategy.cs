using SudokuNexus.Domain.Enums;
using SudokuNexus.Domain.Models;
using SudokuNexus.Domain.Primitives;

namespace SudokuNexus.Engine.Grading.Strategies;

/// <summary>
/// MODULE 3.4: X-Wing Strategy (Weight: 3.2).
/// 
/// Rule (Row-based X-Wing):
/// For a candidate digit d (1..9), look for two distinct Rows (r1 and r2) where digit d
/// appears as a candidate in EXACTLY TWO columns, and those two columns are the SAME
/// (c1 and c2) in both rows!
/// 
/// Visual Diagram:
///       Col c1      Col c2
/// Row r1:  [d] ------- [d]   (d appears ONLY in c1 and c2 in Row r1)
///           |           |
/// Row r2:  [d] ------- [d]   (d appears ONLY in c1 and c2 in Row r2)
/// 
/// Deduction:
/// In r1 and r2, digit d must occupy either (r1,c1)+(r2,c2) or (r1,c2)+(r2,c1).
/// Either way, Columns c1 and c2 now have their d accounted for!
/// Therefore, digit d can be ELIMINATED from all other cells in Column c1 and Column c2
/// (for all rows r != r1 and r != r2).
/// 
/// (The exact same logic applies symmetrically when swapping Rows and Columns).
/// </summary>
public sealed class XWingStrategy : IDeductionStrategy
{
    public TechniqueType Technique => TechniqueType.XWing;
    public double DifficultyWeight => 3.2;

    public SolveStep? FindNextStep(Board board)
    {
        // TODO [Module 3.4]:
        // For `digit` from 1 to 9:
        // 1. Row-based X-Wing:
        //    - For each row `r` (0..8), build a 9-bit column mask `ushort colMask = 0` where bit `c` (0..8)
        //      is set if `board.GetValue(r, c) == 0 && board.GetCandidates(r * 9 + c).HasCandidate(digit)`.
        //    - Find two rows `r1 < r2` where `BitOperations.PopCount(colMask[r1]) == 2` and `colMask[r1] == colMask[r2]`.
        //    - Extract the two column indices `c1` and `c2`.
        //    - Check all other rows `otherRow` (0..8, `otherRow != r1 && otherRow != r2`) in columns `c1` and `c2`:
        //      If `board.GetCandidates(otherRow * 9 + c1).HasCandidate(digit)`, add a `CandidateElimination`.
        //      If `board.GetCandidates(otherRow * 9 + c2).HasCandidate(digit)`, add a `CandidateElimination`.
        //    - If `eliminations.Count > 0`, return a `SolveStep` with the 4 corner cells as `PrimaryCells`!
        // 2. Column-based X-Wing (optional or symmetric):
        //    - Same logic with Columns as the base sets and Rows as the cover sets.
        throw new NotImplementedException("TODO [Module 3.4]: Implement XWingStrategy.FindNextStep.");
    }
}
