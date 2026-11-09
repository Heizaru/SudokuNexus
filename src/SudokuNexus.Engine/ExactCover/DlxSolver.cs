using System.Diagnostics;
using SudokuNexus.Domain.Models;

namespace SudokuNexus.Engine.ExactCover;

/// <summary>
/// Result of running the Dancing Links (Algorithm X) solver on a Sudoku board.
/// </summary>
public sealed record DlxSolveResult(
    bool IsSolved,
    bool HasUniqueSolution,
    int SolutionCount,
    string? SolvedBoardString,
    double ElapsedMicroseconds
);

/// <summary>
/// Exact cover Sudoku solver using Donald Knuth's Algorithm X and Dancing Links (DLX).
/// Maps a 9x9 Sudoku grid to a 729x324 exact cover matrix (cell, row-digit, column-digit, and box-digit constraints).
/// </summary>
public sealed class DlxSolver
{
    private const int TotalColumns = 324;
    private const int TotalRows = 729;

    /// <summary>
    /// Solves the given <paramref name="board"/> using Dancing Links and checks whether the solution is unique.
    /// </summary>
    public DlxSolveResult Solve(Board board, bool checkUniqueness = true)
    {
        long startTicks = Stopwatch.GetTimestamp();

        int maxSolutions = checkUniqueness ? 2 : 1;
        var solutions = new List<int[]>(maxSolutions);

        RunExactCover(board, maxSolutions, solutions);

        double elapsedMicros = Stopwatch.GetElapsedTime(startTicks).TotalMicroseconds;

        if (solutions.Count == 0)
        {
            return new DlxSolveResult(
                IsSolved: false,
                HasUniqueSolution: false,
                SolutionCount: 0,
                SolvedBoardString: null,
                ElapsedMicroseconds: elapsedMicros);
        }

        string solvedString = ConvertSolutionRowsToString(solutions[0]);
        return new DlxSolveResult(
            IsSolved: true,
            HasUniqueSolution: solutions.Count == 1,
            SolutionCount: solutions.Count,
            SolvedBoardString: solvedString,
            ElapsedMicroseconds: elapsedMicros);
    }

    /// <summary>
    /// Counts the number of valid solutions for <paramref name="board"/> up to <paramref name="limit"/>.
    /// Passing <c>limit = 2</c> is the fastest way to prove a puzzle has a unique solution
    /// </summary>
    public int CountSolutions(Board board, int limit = 2)
    {
        var solutions = new List<int[]>(limit);
        RunExactCover(board, limit, solutions);
        return solutions.Count;
    }

    private void RunExactCover(Board board, int maxSolutions, List<int[]> solutions)
    {
        ColumnNode header = BuildSudokuToroidalMatrix(out DlxNode?[] rowFirstNodes);
        var currentSolution = new List<int>(81);

        // Pre-cover columns corresponding to initial clues already placed on the board
        for (var cellIndex = 0; cellIndex < SudokuTopology.TotalCells; cellIndex++)
        {
            int placedDigit = board.GetValue(cellIndex);
            if (placedDigit == 0) continue;
            int rowId = cellIndex * 9 + (placedDigit - 1);
            DlxNode? firstNode = rowFirstNodes[rowId];
            if (firstNode is null) continue;
            currentSolution.Add(rowId);
            CoverRowClue(firstNode);
        }

        Search(header, currentSolution, solutions, maxSolutions);
    }

    /// <summary>
    /// Covers a constraint column and removes all conflicting rows from the toroidal matrix.
    /// </summary>
    public static void Cover(ColumnNode column)
    {
        column.Right.Left = column.Left;
        column.Left.Right = column.Right;

        for (DlxNode i = column.Down; i != column; i = i.Down)
        {
            for (DlxNode j = i.Right; j != i; j = j.Right)
            {
                j.Down.Up = j.Up;
                j.Up.Down = j.Down;
                j.Column.Size--;
            }
        }
    }

    /// <summary>
    /// Uncovers a constraint column in reverse order, restoring all linked rows and nodes.
    /// </summary>
    public static void Uncover(ColumnNode column)
    {
        for (DlxNode i = column.Up; i != column; i = i.Up)
        {
            for (DlxNode j = i.Left; j != i; j = j.Left)
            {
                j.Column.Size++;
                j.Down.Up = j;
                j.Up.Down = j;
            }
        }

        column.Right.Left = column;
        column.Left.Right = column;
    }

    /// <summary>
    /// Recursive Algorithm X Search with minimum column size.
    /// </summary>
    private static void Search(
        ColumnNode header,
        List<int> currentSolution,
        List<int[]> solutions,
        int maxSolutions)
    {
        if (header.Right == header)
        {
            solutions.Add([.. currentSolution]);
            return;
        }

        // Choose the column with the smallest size
        ColumnNode c = (ColumnNode)header.Right;
        for (var col = (ColumnNode)header.Right; col != header; col = (ColumnNode)col.Right)
        {
            if (col.Size < c.Size)
            {
                c = col;
            }
        }

        // No need to look further, this path isn't possible.
        if (c.Size == 0)
        {
            return;
        }

        Cover(c);

        for (DlxNode r = c.Down; r != c; r = r.Down)
        {
            currentSolution.Add(r.RowId);

            for (DlxNode j = r.Right; j != r; j = j.Right)
            {
                Cover(j.Column);
            }

            Search(header, currentSolution, solutions, maxSolutions);

            for (DlxNode j = r.Left; j != r; j = j.Left)
            {
                Uncover(j.Column);
            }

            currentSolution.RemoveAt(currentSolution.Count - 1);

            if (solutions.Count >= maxSolutions)
            {
                break;
            }
        }

        Uncover(c);
    }

    private static void CoverRowClue(DlxNode firstNode)
    {
        Cover(firstNode.Column);
        for (DlxNode j = firstNode.Right; j != firstNode; j = j.Right)
        {
            Cover(j.Column);
        }
    }

    /// <summary>
    /// Builds the 729 × 324 toroidal doubly-linked list representing all possible Sudoku placements.
    /// </summary>
    public static ColumnNode BuildSudokuToroidalMatrix(out DlxNode?[] rowFirstNodes)
    {
        var header = new ColumnNode(-1);
        var columns = new ColumnNode[TotalColumns];

        ColumnNode prev = header;
        for (int c = 0; c < TotalColumns; c++)
        {
            var col = new ColumnNode(c);
            columns[c] = col;

            col.Left = prev;
            col.Right = header;
            prev.Right = col;
            header.Left = col;
            prev = col;
        }

        rowFirstNodes = new DlxNode?[TotalRows];
        Span<int> constraintCols = stackalloc int[4];

        for (int r = 0; r < 9; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                int cellIndex = r * 9 + c;
                int boxIndex = (r / 3) * 3 + (c / 3);

                for (int d = 0; d < 9; d++)
                {
                    int rowId = cellIndex * 9 + d;

                    constraintCols[0] = cellIndex;
                    constraintCols[1] = 81 + r * 9 + d;
                    constraintCols[2] = 162 + c * 9 + d;
                    constraintCols[3] = 243 + boxIndex * 9 + d;

                    DlxNode? firstInRow = null;
                    for (int k = 0; k < 4; k++)
                    {
                        ColumnNode colNode = columns[constraintCols[k]];
                        var node = new DlxNode
                        {
                            Column = colNode,
                            RowId = rowId
                        };

                        // Link vertically at the bottom of colNode
                        node.Up = colNode.Up;
                        node.Down = colNode;
                        colNode.Up.Down = node;
                        colNode.Up = node;
                        colNode.Size++;

                        // Link horizontally in the row
                        if (firstInRow is null)
                        {
                            firstInRow = node;
                        }
                        else
                        {
                            node.Left = firstInRow.Left;
                            node.Right = firstInRow;
                            firstInRow.Left.Right = node;
                            firstInRow.Left = node;
                        }
                    }

                    rowFirstNodes[rowId] = firstInRow;
                }
            }
        }

        return header;
    }

    private static string ConvertSolutionRowsToString(int[] rowIds)
    {
        Span<char> chars = stackalloc char[SudokuTopology.TotalCells];
        chars.Fill('.');
        foreach (int rowId in rowIds)
        {
            int cellIndex = rowId / 9;
            int digit = (rowId % 9) + 1;
            chars[cellIndex] = (char)('0' + digit);
        }
        return new string(chars);
    }
}
