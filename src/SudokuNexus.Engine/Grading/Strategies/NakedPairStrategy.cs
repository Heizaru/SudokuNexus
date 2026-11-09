using SudokuNexus.Domain.Enums;
using SudokuNexus.Domain.Models;
using SudokuNexus.Domain.Primitives;

namespace SudokuNexus.Engine.Grading.Strategies;

/// <summary>
/// MODULE 3.3: Naked Pair Strategy (Weight: 3.0).
/// 
/// Rule:
/// If two cells in the same House (Row, Column, or 3x3 Box) have the EXACT SAME
/// <see cref="CandidateMask"/> with <c>Count == 2</c> (for example, both cells can only be {4, 7}),
/// then those two digits must occupy those two cells.
/// Therefore, neither digit can appear in any OTHER empty cell in that House!
/// 
/// Tip: Notice how clean this is with bitmasks!
/// Two cells <c>cellA</c> and <c>cellB</c> form a Naked Pair if:
/// <c>maskA.Count == 2 &amp;&amp; maskA == maskB</c>.
/// And another cell <c>otherCell</c> in the house has candidates to eliminate if:
/// <c>(board.GetCandidates(otherCell) &amp; maskA).Bits != 0</c>!
/// </summary>
public sealed class NakedPairStrategy : IDeductionStrategy
{
    public TechniqueType Technique => TechniqueType.NakedPair;
    public double DifficultyWeight => 3.0;

    private static readonly HouseType[] AllHouseTypes = [HouseType.Row, HouseType.Column, HouseType.Box];

    public SolveStep? FindNextStep(Board board)
    {
        // TODO [Module 3.3]:
        // Iterate over each `houseType` in `AllHouseTypes` and `houseIndex` in 0..8:
        // 1. Get the 9 cells of the house: `ReadOnlySpan<int> houseCells = SudokuTopology.GetHouseCells(houseType, houseIndex)`.
        // 2. Find all pairs of cells `(cellA, cellB)` in `houseCells` where:
        //    - `board.GetValue(cellA) == 0`
        //    - `CandidateMask mask = board.GetCandidates(cellA)` has `mask.Count == 2`
        //    - `board.GetCandidates(cellB) == mask`
        // 3. For such a pair, check all OTHER empty cells `peer` in `houseCells` (`peer != cellA && peer != cellB`):
        //    - For each digit `d` in `mask.ToDigits()`:
        //      If `board.GetCandidates(peer).HasCandidate(d)`, add `new CandidateElimination(peer, d)` to an eliminations list.
        // 4. IMPORTANT: Only return a `SolveStep` if `eliminations.Count > 0`!
        //    (If the pair's digits are already eliminated from all other cells in the house, keep searching so we don't loop forever).
        // 5. Return `new SolveStep(Technique, DifficultyWeight, description, [cellA, cellB], null, null, eliminations)`.
        throw new NotImplementedException("TODO [Module 3.3]: Implement NakedPairStrategy.FindNextStep using CandidateMask equality.");
    }
}
