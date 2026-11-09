using SudokuNexus.Domain.Enums;

namespace SudokuNexus.Domain.Models;

/// <summary>
/// Precomputed static lookup tables for a standard 9x9 Sudoku grid (81 cells).
/// 
/// Every cell belongs to 3 "Houses" (a Row, a Column, and a 3x3 Box) and has exactly
/// 20 distinct "Peers" (other cells that share at least one House with it).
/// Precomputing these once in a static constructor avoids recalculating row/col/box
/// math inside loops.
/// </summary>
public static class SudokuTopology
{
    public const int GridSize = 9;
    public const int TotalCells = 81;
    public const int PeerCount = 20;

    /// <summary>
    /// RowIndices[row] contains the 9 cell indices (0..80) belonging to <c>row</c> (0..8).
    /// </summary>
    public static readonly int[][] RowIndices = new int[GridSize][];

    /// <summary>
    /// ColumnIndices[col] contains the 9 cell indices (0..80) belonging to <c>col</c> (0..8).
    /// </summary>
    public static readonly int[][] ColumnIndices = new int[GridSize][];

    /// <summary>
    /// BoxIndices[box] contains the 9 cell indices (0..80) belonging to 3x3 <c>box</c> (0..8).
    /// </summary>
    public static readonly int[][] BoxIndices = new int[GridSize][];

    /// <summary>
    /// Peers[cellIndex] contains the 20 distinct peer cell indices that share a Row, Column, or Box with <c>cellIndex</c>.
    /// </summary>
    public static readonly int[][] Peers = new int[TotalCells][];

    static SudokuTopology()
    {
        for (int i = 0; i < GridSize; i++)
        {
            RowIndices[i] = new int[GridSize];
            ColumnIndices[i] = new int[GridSize];
            BoxIndices[i] = new int[GridSize];
        }

        for (int r = 0; r < GridSize; r++)
        {
            for (int c = 0; c < GridSize; c++)
            {
                int idx = r * GridSize + c;
                RowIndices[r][c] = idx;
                ColumnIndices[c][r] = idx;

                int box = (r / 3) * 3 + (c / 3);
                int boxSlot = (r % 3) * 3 + (c % 3);
                BoxIndices[box][boxSlot] = idx;
            }
        }

        for (int idx = 0; idx < TotalCells; idx++)
        {
            int r = idx / GridSize;
            int c = idx % GridSize;
            int box = (r / 3) * 3 + (c / 3);

            var peerSet = new HashSet<int>(PeerCount);
            foreach (int p in RowIndices[r])
            {
                if (p != idx) peerSet.Add(p);
            }
            foreach (int p in ColumnIndices[c])
            {
                if (p != idx) peerSet.Add(p);
            }
            foreach (int p in BoxIndices[box])
            {
                if (p != idx) peerSet.Add(p);
            }

            Peers[idx] = peerSet.OrderBy(x => x).ToArray();
        }
    }

    public static ReadOnlySpan<int> GetHouseCells(HouseType houseType, int houseIndex) =>
        houseType switch
        {
            HouseType.Row => RowIndices[houseIndex],
            HouseType.Column => ColumnIndices[houseIndex],
            HouseType.Box => BoxIndices[houseIndex],
            _ => throw new ArgumentOutOfRangeException(nameof(houseType))
        };
}
