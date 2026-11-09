namespace SudokuNexus.Engine.ExactCover;

/// <summary>
/// A node in Donald Knuth's toroidal doubly-linked list for Dancing Links (DLX).
/// Each node maintains 4 directional references (<see cref="Left"/>, <see cref="Right"/>,
/// <see cref="Up"/>, <see cref="Down"/>), a reference to its <see cref="Column"/> header,
/// and the <see cref="RowId"/> (encoding CellIndex and Digit for Sudoku).
/// </summary>
public class DlxNode
{
    public DlxNode Left { get; set; }
    public DlxNode Right { get; set; }
    public DlxNode Up { get; set; }
    public DlxNode Down { get; set; }
    public ColumnNode Column { get; set; }
    public int RowId { get; init; }

    public DlxNode()
    {
        Left = this;
        Right = this;
        Up = this;
        Down = this;
        Column = null!;
    }
}

/// <summary>
/// Specialized header node for each of the 324 Exact Cover constraint columns.
/// Tracks <see cref="Size"/> (number of active 1s in the column) so the S-Heuristic
/// can pick the most constrained column in O(columns) time.
/// </summary>
public sealed class ColumnNode : DlxNode
{
    public int ColumnIndex { get; }
    public int Size { get; set; }

    public ColumnNode(int columnIndex)
    {
        ColumnIndex = columnIndex;
        Column = this;
    }
}
