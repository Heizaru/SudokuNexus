using System.Numerics;
using SudokuNexus.Domain.Enums;
using SudokuNexus.Domain.Models;
using SudokuNexus.Domain.Primitives;

namespace SudokuNexus.Engine.Grading.Strategies;

/// <summary>
/// X-Wing Strategy (Weight: 3.2).
/// Detects when a candidate digit is restricted to the same two columns across two rows (or two rows across two columns),
/// eliminating that digit from all other cells in the intersecting cover lines.
/// </summary>
public sealed class XWingStrategy : IDeductionStrategy
{
    private const TechniqueType StrategyTechnique = TechniqueType.XWing;
    private const double StrategyWeight = 3.2;

    public TechniqueType Technique => StrategyTechnique;
    public double DifficultyWeight => StrategyWeight;

    public SolveStep? FindNextStep(Board board)
    {
        for (int digit = 1; digit <= 9; digit++)
        {
            // 1. Row-based X-Wing:
            // Two rows (r1, r2) where digit appears in the exact same two columns (c1, c2).
            SolveStep? step = FindRowXWing(board, digit);
            if (step is not null) return step;

            // 2. Column-based X-Wing (symmetric):
            // Two columns (c1, c2) where digit appears in the exact same two rows (r1, r2).
            step = FindColumnXWing(board, digit);
            if (step is not null) return step;
        }

        return null;
    }

    private static SolveStep? FindRowXWing(Board board, int digit)
    {
        Span<ushort> colMasks = stackalloc ushort[9];

        for (int r = 0; r < 9; r++)
        {
            ushort mask = 0;
            for (int c = 0; c < 9; c++)
            {
                int cell = r * 9 + c;
                if (board.GetValue(cell) == 0 && board.GetCandidates(cell).HasCandidate(digit))
                {
                    mask |= (ushort)(1 << c);
                }
            }
            colMasks[r] = mask;
        }

        for (int r1 = 0; r1 < 8; r1++)
        {
            ushort mask = colMasks[r1];
            if (BitOperations.PopCount(mask) != 2) continue;

            for (int r2 = r1 + 1; r2 < 9; r2++)
            {
                if (colMasks[r2] != mask) continue;

                // Extract the two column indices from the 2-bit mask
                int c1 = BitOperations.TrailingZeroCount(mask);
                int c2 = 31 - BitOperations.LeadingZeroCount(mask);

                // Eliminate digit from all other rows in columns c1 and c2
                var eliminations = new List<CandidateElimination>();
                for (int otherRow = 0; otherRow < 9; otherRow++)
                {
                    if (otherRow == r1 || otherRow == r2) continue;

                    int cell1 = otherRow * 9 + c1;
                    if (board.GetValue(cell1) == 0 && board.GetCandidates(cell1).HasCandidate(digit))
                    {
                        eliminations.Add(new CandidateElimination(cell1, digit));
                    }

                    int cell2 = otherRow * 9 + c2;
                    if (board.GetValue(cell2) == 0 && board.GetCandidates(cell2).HasCandidate(digit))
                    {
                        eliminations.Add(new CandidateElimination(cell2, digit));
                    }
                }

                if (eliminations.Count > 0)
                {
                    int[] primaryCells = [r1 * 9 + c1, r1 * 9 + c2, r2 * 9 + c1, r2 * 9 + c2];
                    string description =
                        $"X-Wing on digit {digit} in Rows {r1 + 1} and {r2 + 1} across Columns {c1 + 1} and {c2 + 1} eliminates {digit} from {eliminations.Count} cell(s).";

                    return new SolveStep(
                        StrategyTechnique,
                        StrategyWeight,
                        description,
                        PrimaryCells: primaryCells,
                        PlacedCellIndex: null,
                        PlacedDigit: null,
                        Eliminations: eliminations);
                }
            }
        }

        return null;
    }

    private static SolveStep? FindColumnXWing(Board board, int digit)
    {
        Span<ushort> rowMasks = stackalloc ushort[9];

        for (int c = 0; c < 9; c++)
        {
            ushort mask = 0;
            for (int r = 0; r < 9; r++)
            {
                int cell = r * 9 + c;
                if (board.GetValue(cell) == 0 && board.GetCandidates(cell).HasCandidate(digit))
                {
                    mask |= (ushort)(1 << r);
                }
            }
            rowMasks[c] = mask;
        }

        for (int c1 = 0; c1 < 8; c1++)
        {
            ushort mask = rowMasks[c1];
            if (BitOperations.PopCount(mask) != 2) continue;

            for (int c2 = c1 + 1; c2 < 9; c2++)
            {
                if (rowMasks[c2] != mask) continue;

                // Extract the two row indices from the 2-bit mask
                int r1 = BitOperations.TrailingZeroCount(mask);
                int r2 = 31 - BitOperations.LeadingZeroCount(mask);

                // Eliminate digit from all other columns in rows r1 and r2
                var eliminations = new List<CandidateElimination>();
                for (int otherCol = 0; otherCol < 9; otherCol++)
                {
                    if (otherCol == c1 || otherCol == c2) continue;

                    int cell1 = r1 * 9 + otherCol;
                    if (board.GetValue(cell1) == 0 && board.GetCandidates(cell1).HasCandidate(digit))
                    {
                        eliminations.Add(new CandidateElimination(cell1, digit));
                    }

                    int cell2 = r2 * 9 + otherCol;
                    if (board.GetValue(cell2) == 0 && board.GetCandidates(cell2).HasCandidate(digit))
                    {
                        eliminations.Add(new CandidateElimination(cell2, digit));
                    }
                }

                if (eliminations.Count > 0)
                {
                    int[] primaryCells = [r1 * 9 + c1, r1 * 9 + c2, r2 * 9 + c1, r2 * 9 + c2];
                    string description =
                        $"X-Wing on digit {digit} in Columns {c1 + 1} and {c2 + 1} across Rows {r1 + 1} and {r2 + 1} eliminates {digit} from {eliminations.Count} cell(s).";

                    return new SolveStep(
                        StrategyTechnique,
                        StrategyWeight,
                        description,
                        PrimaryCells: primaryCells,
                        PlacedCellIndex: null,
                        PlacedDigit: null,
                        Eliminations: eliminations);
                }
            }
        }

        return null;
    }
}
