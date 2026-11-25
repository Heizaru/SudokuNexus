using SudokuNexus.Domain.Enums;
using SudokuNexus.Domain.Models;
using SudokuNexus.Domain.Primitives;

namespace SudokuNexus.Engine.Grading.Strategies;

/// <summary>
/// Naked Pair Strategy (Weight: 3.0).
/// If two cells in the same house share an identical 2-candidate mask, those two digits are eliminated
/// from all other empty cells in that house.
/// </summary>
public sealed class NakedPairStrategy : IDeductionStrategy
{
    public TechniqueType Technique => TechniqueType.NakedPair;
    public double DifficultyWeight => 3.0;

    private static readonly HouseType[] AllHouseTypes = [HouseType.Row, HouseType.Column, HouseType.Box];

    public SolveStep? FindNextStep(Board board)
    {
        foreach (HouseType houseType in AllHouseTypes)
        {
            for (var houseIndex = 0; houseIndex < 9; houseIndex++)
            {
                ReadOnlySpan<int> houseCells = SudokuTopology.GetHouseCells(houseType, houseIndex);

                // Check all unique pairs of cells in this house
                for (var i = 0; i < houseCells.Length; i++)
                {
                    int cellA = houseCells[i];
                    if (board.GetValue(cellA) != 0) continue;

                    CandidateMask maskA = board.GetCandidates(cellA);
                    if (maskA.Count != 2) continue;

                    for (int j = i + 1; j < houseCells.Length; j++)
                    {
                        int cellB = houseCells[j];
                        if (board.GetValue(cellB) != 0) continue;

                        CandidateMask maskB = board.GetCandidates(cellB);
                        if (maskA != maskB) continue;

                        // Found a Naked Pair (cellA, cellB) with maskA!
                        // Now collect eliminations from all OTHER cells in this house.
                        List<CandidateElimination>? eliminations = null;

                        for (var k = 0; k < houseCells.Length; k++)
                        {
                            int peer = houseCells[k];
                            if (peer == cellA || peer == cellB) continue;
                            if (board.GetValue(peer) != 0) continue;

                            CandidateMask peerMask = board.GetCandidates(peer);
                            CandidateMask commonCandidates = peerMask & maskA;
                            if (commonCandidates.Bits == 0) continue;

                            eliminations ??= new List<CandidateElimination>();
                            foreach (int digit in commonCandidates.ToDigits())
                            {
                                eliminations.Add(new CandidateElimination(peer, digit));
                            }
                        }

                        // Only return a step if it actually eliminates at least one candidate
                        if (eliminations is { Count: > 0 })
                        {
                            IReadOnlyList<int> digits = maskA.ToDigits();
                            var description =
                                $"Naked Pair {{{digits[0]},{digits[1]}}} in {houseType} {houseIndex + 1} at cells ({cellA},{cellB}) eliminates candidates from other cells.";

                            return new SolveStep(
                                Technique,
                                DifficultyWeight,
                                description,
                                PrimaryCells: [cellA, cellB],
                                PlacedCellIndex: null,
                                PlacedDigit: null,
                                Eliminations: eliminations);
                        }
                    }
                }
            }
        }

        return null;
    }
}
