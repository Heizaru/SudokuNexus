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
/// MODULE 2: Donald Knuth's Algorithm X implemented with Dancing Links (DLX).
/// 
/// How Sudoku Maps to Exact Cover:
/// - There are 9 × 9 × 9 = 729 possible candidate placements: placing digit d (1..9) in cell (r, c).
/// - Each placement satisfies exactly 4 constraints out of 324 total columns:
///   1. Cell constraint [0..80]:       Every cell (r, c) must contain exactly one digit.
///      Column = r * 9 + c
///   2. Row-Digit constraint [81..161]: Every row r must contain each digit d (0..8) once.
///      Column = 81 + r * 9 + (d - 1)
///   3. Col-Digit constraint [162..242]: Every column c must contain each digit d (0..8) once.
///      Column = 162 + c * 9 + (d - 1)
///   4. Box-Digit constraint [243..323]: Every 3x3 box b must contain each digit d (0..8) once.
///      Column = 243 + b * 9 + (d - 1)
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
    /// Passing <c>limit = 2</c> is the fastest way to prove a puzzle has a unique solution (count == 1).
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
        for (int cellIndex = 0; cellIndex < SudokuTopology.TotalCells; cellIndex++)
        {
            int placedDigit = board.GetValue(cellIndex);
            if (placedDigit != 0)
            {
                int rowId = cellIndex * 9 + (placedDigit - 1);
                DlxNode? firstNode = rowFirstNodes[rowId];
                if (firstNode is not null)
                {
                    currentSolution.Add(rowId);
                    CoverRowClue(firstNode);
                }
            }
        }

        Search(header, currentSolution, solutions, maxSolutions);
    }

    /// <summary>
    /// MODULE 2.1: Covers a constraint column <paramref name="column"/> in the toroidal matrix.
    /// 
    /// Knuth's Cover(c) specification:
    /// 1. Unlink <paramref name="column"/> horizontally from its neighbors:
    ///    <c>column.Right.Left = column.Left;</c>
    ///    <c>column.Left.Right = column.Right;</c>
    /// 2. For each node <c>i = column.Down</c> while <c>i != column</c> (moving Down):
    ///    For each node <c>j = i.Right</c> while <c>j != i</c> (moving Right across the row):
    ///      Unlink <c>j</c> vertically from its column:
    ///      <c>j.Down.Up = j.Up;</c>
    ///      <c>j.Up.Down = j.Down;</c>
    ///      Decrement <c>j.Column.Size--</c>.
    /// </summary>
    public static void Cover(ColumnNode column)
    {
        // TODO [Module 2.1]: Implement Knuth's Cover(c) operation on the toroidal doubly-linked list.
        throw new NotImplementedException("TODO [Module 2.1]: Implement DlxSolver.Cover(ColumnNode column).");
    }

    /// <summary>
    /// MODULE 2.1: Uncovers a constraint column <paramref name="column"/> in the exact reverse order of <see cref="Cover"/>.
    /// 
    /// Knuth's Uncover(c) specification:
    /// 1. For each node <c>i = column.Up</c> while <c>i != column</c> (moving Up!):
    ///    For each node <c>j = i.Left</c> while <c>j != i</c> (moving Left!):
    ///      Increment <c>j.Column.Size++</c>.
    ///      Relink <c>j</c> vertically into its column:
    ///      <c>j.Down.Up = j;</c>
    ///      <c>j.Up.Down = j;</c>
    /// 2. Relink <paramref name="column"/> horizontally:
    ///    <c>column.Right.Left = column;</c>
    ///    <c>column.Left.Right = column;</c>
    /// </summary>
    public static void Uncover(ColumnNode column)
    {
        // TODO [Module 2.1]: Implement Knuth's Uncover(c) operation (must traverse Up and Left!).
        throw new NotImplementedException("TODO [Module 2.1]: Implement DlxSolver.Uncover(ColumnNode column).");
    }

    /// <summary>
    /// MODULE 2.2: Recursive Algorithm X Search with the S-Heuristic (minimum column size).
    /// </summary>
    private static void Search(
        ColumnNode header,
        List<int> currentSolution,
        List<int[]> solutions,
        int maxSolutions)
    {
        // TODO [Module 2.2]:
        // 1. Base case: If `header.Right == header`, all 324 constraints are satisfied!
        //    Add `currentSolution.ToArray()` to `solutions` and return.
        // 2. Choose the column `c` with the smallest `Size` (S-heuristic) by iterating
        //    from `var col = (ColumnNode)header.Right` while `col != header`.
        //    - Optimization: If `c.Size == 0`, this branch is a dead end — return immediately!
        // 3. Call `Cover(c)`.
        // 4. For each row node `r = c.Down` while `r != c`:
        //    a. Push `r.RowId` to `currentSolution`.
        //    b. For each node `j = r.Right` while `j != r`: call `Cover(j.Column)`.
        //    c. Recursively call `Search(header, currentSolution, solutions, maxSolutions)`.
        //    d. For each node `j = r.Left` while `j != r`: call `Uncover(j.Column)`.
        //    e. Pop the last item from `currentSolution`.
        //    f. Early exit: if `solutions.Count >= maxSolutions`, break out of the loop!
        // 5. Call `Uncover(c)`.
        throw new NotImplementedException("TODO [Module 2.2]: Implement DlxSolver.Search with S-heuristic.");
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
    /// (Provided complete so you can focus on Cover, Uncover, and Search).
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
