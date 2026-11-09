namespace SudokuNexus.Domain.Primitives;

/// <summary>
/// A lightweight value type representing a cell's position on the 9x9 Sudoku grid.
/// Index is in [0..80], Row in [0..8], Column in [0..8], and Box in [0..8].
/// </summary>
public readonly record struct CellCoordinate(int Index)
{
    public int Row => Index / 9;
    public int Column => Index % 9;
    public int Box => (Row / 3) * 3 + (Column / 3);

    public static CellCoordinate FromRowCol(int row, int col)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(row);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(row, 8);
        ArgumentOutOfRangeException.ThrowIfNegative(col);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(col, 8);
        return new CellCoordinate(row * 9 + col);
    }

    /// <summary>
    /// Standard Sudoku notation (e.g., "R1C1" for top-left, "R9C9" for bottom-right).
    /// </summary>
    public override string ToString() => $"R{Row + 1}C{Column + 1}";
}
