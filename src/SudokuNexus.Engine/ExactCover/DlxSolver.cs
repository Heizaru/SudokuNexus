using System.Diagnostics;
using SudokuNexus.Domain.Models;

namespace SudokuNexus.Engine.ExactCover;

/// <summary>
/// Result of running the Dancing Links (Algorithm X) solver on a Sudoku board.
/// </summary>
public readonly record struct DlxSolveResult(
    bool IsSolved,
    bool HasUniqueSolution,
    int SolutionCount,
    string? SolvedBoardString,
    double ElapsedMicroseconds
);

/// <summary>
/// Exact cover Sudoku solver using Donald Knuth's Algorithm X and Dancing Links (DLX).
/// Maps a 9x9 Sudoku grid to a 729x324 exact cover matrix (cell, row-digit, column-digit, and box-digit constraints)
/// using a precomputed static template and stack-allocated flat index arrays for zero-allocation solving.
/// </summary>
public sealed class DlxSolver
{
    public const int HeaderNode = 0;
    public const int TotalColumns = 324;
    public const int TotalRows = 729;
    public const int TotalNodes = 1 + TotalColumns + TotalRows * 4; // 3,241 nodes

    private static readonly short[] TemplateLeft = new short[TotalNodes];
    private static readonly short[] TemplateRight = new short[TotalNodes];
    private static readonly short[] TemplateUp = new short[TotalNodes];
    private static readonly short[] TemplateDown = new short[TotalNodes];
    private static readonly short[] TemplateCol = new short[TotalNodes];
    private static readonly short[] TemplateRowId = new short[TotalNodes];
    private static readonly short[] TemplateSize = new short[TotalColumns + 1];
    private static readonly short[] RowFirstNode = new short[TotalRows];

    static DlxSolver()
    {
        // Initialize header node (index 0) and 324 column headers (indices 1..324)
        short prev = HeaderNode;
        for (short c = 1; c <= TotalColumns; c++)
        {
            TemplateLeft[c] = prev;
            TemplateRight[c] = HeaderNode;
            TemplateRight[prev] = c;
            TemplateLeft[HeaderNode] = c;

            TemplateUp[c] = c;
            TemplateDown[c] = c;
            TemplateCol[c] = c;
            TemplateRowId[c] = -1;
            TemplateSize[c] = 0;
            prev = c;
        }

        short nextNode = TotalColumns + 1;
        Span<int> constraintCols = stackalloc int[4];

        for (int r = 0; r < 9; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                int cellIndex = r * 9 + c;
                int boxIndex = (r / 3) * 3 + (c / 3);

                for (int d = 0; d < 9; d++)
                {
                    short rowId = (short)(cellIndex * 9 + d);

                    // 1-based column header indices (1..324)
                    constraintCols[0] = 1 + cellIndex;
                    constraintCols[1] = 1 + 81 + r * 9 + d;
                    constraintCols[2] = 1 + 162 + c * 9 + d;
                    constraintCols[3] = 1 + 243 + boxIndex * 9 + d;

                    short firstInRow = 0;
                    for (int k = 0; k < 4; k++)
                    {
                        short colNode = (short)constraintCols[k];
                        short node = nextNode++;

                        TemplateCol[node] = colNode;
                        TemplateRowId[node] = rowId;

                        // Link vertically at the bottom of colNode
                        short upNode = TemplateUp[colNode];
                        TemplateUp[node] = upNode;
                        TemplateDown[node] = colNode;
                        TemplateDown[upNode] = node;
                        TemplateUp[colNode] = node;
                        TemplateSize[colNode]++;

                        // Link horizontally in the row
                        if (firstInRow == 0)
                        {
                            firstInRow = node;
                            TemplateLeft[node] = node;
                            TemplateRight[node] = node;
                        }
                        else
                        {
                            short leftNode = TemplateLeft[firstInRow];
                            TemplateLeft[node] = leftNode;
                            TemplateRight[node] = firstInRow;
                            TemplateRight[leftNode] = node;
                            TemplateLeft[firstInRow] = node;
                        }
                    }

                    RowFirstNode[rowId] = firstInRow;
                }
            }
        }
    }

    /// <summary>
    /// Solves the given <paramref name="board"/> using Dancing Links and checks whether the solution is unique.
    /// </summary>
    public DlxSolveResult Solve(Board board, bool checkUniqueness = true)
    {
        long startTicks = Stopwatch.GetTimestamp();

        int maxSolutions = checkUniqueness ? 2 : 1;
        Span<int> firstSolutionRows = stackalloc int[SudokuTopology.TotalCells];
        int solutionCount = RunExactCover(board, maxSolutions, firstSolutionRows);

        double elapsedMicros = Stopwatch.GetElapsedTime(startTicks).TotalMicroseconds;

        if (solutionCount == 0)
        {
            return new DlxSolveResult(
                IsSolved: false,
                HasUniqueSolution: false,
                SolutionCount: 0,
                SolvedBoardString: null,
                ElapsedMicroseconds: elapsedMicros);
        }

        string solvedString = ConvertSolutionRowsToString(firstSolutionRows);
        return new DlxSolveResult(
            IsSolved: true,
            HasUniqueSolution: solutionCount == 1,
            SolutionCount: solutionCount,
            SolvedBoardString: solvedString,
            ElapsedMicroseconds: elapsedMicros);
    }

    /// <summary>
    /// Counts the number of valid solutions for <paramref name="board"/> up to <paramref name="limit"/>.
    /// Passing <c>limit = 2</c> is the fastest way to prove a puzzle has a unique solution.
    /// </summary>
    public static int CountSolutions(Board board, int limit = 2)
    {
        Span<int> unusedSolution = stackalloc int[SudokuTopology.TotalCells];
        return RunExactCover(board, limit, unusedSolution);
    }

    private static int RunExactCover(Board board, int maxSolutions, Span<int> firstSolutionRows)
    {
        Span<short> left = stackalloc short[TotalNodes];
        Span<short> right = stackalloc short[TotalNodes];
        Span<short> up = stackalloc short[TotalNodes];
        Span<short> down = stackalloc short[TotalNodes];
        Span<short> size = stackalloc short[TotalColumns + 1];

        TemplateLeft.AsSpan().CopyTo(left);
        TemplateRight.AsSpan().CopyTo(right);
        TemplateUp.AsSpan().CopyTo(up);
        TemplateDown.AsSpan().CopyTo(down);
        TemplateSize.AsSpan().CopyTo(size);

        Span<int> currentSolution = stackalloc int[SudokuTopology.TotalCells];
        int depth = 0;

        // Pre-cover columns corresponding to initial clues already placed on the board
        for (int cellIndex = 0; cellIndex < SudokuTopology.TotalCells; cellIndex++)
        {
            int placedDigit = board.GetValue(cellIndex);
            if (placedDigit == 0) continue;

            int rowId = cellIndex * 9 + (placedDigit - 1);
            short firstNode = RowFirstNode[rowId];
            currentSolution[depth++] = rowId;

            Cover(TemplateCol[firstNode], left, right, up, down, size);
            for (short j = right[firstNode]; j != firstNode; j = right[j])
            {
                Cover(TemplateCol[j], left, right, up, down, size);
            }
        }

        int solutionCount = 0;
        Search(left, right, up, down, size, currentSolution, depth, maxSolutions, ref solutionCount, firstSolutionRows);
        return solutionCount;
    }

    /// <summary>
    /// Covers a constraint column <paramref name="col"/> in the flat array arena.
    /// </summary>
    public static void Cover(
        short col,
        Span<short> left,
        Span<short> right,
        Span<short> up,
        Span<short> down,
        Span<short> size)
    {
        short l = left[col];
        short r = right[col];
        left[r] = l;
        right[l] = r;

        for (short i = down[col]; i != col; i = down[i])
        {
            for (short j = right[i]; j != i; j = right[j])
            {
                short u = up[j];
                short d = down[j];
                up[d] = u;
                down[u] = d;
                size[TemplateCol[j]]--;
            }
        }
    }

    /// <summary>
    /// Uncovers a constraint column <paramref name="col"/> in reverse order in the flat array arena.
    /// </summary>
    public static void Uncover(
        short col,
        Span<short> left,
        Span<short> right,
        Span<short> up,
        Span<short> down,
        Span<short> size)
    {
        for (short i = up[col]; i != col; i = up[i])
        {
            for (short j = left[i]; j != i; j = left[j])
            {
                size[TemplateCol[j]]++;
                short u = up[j];
                short d = down[j];
                up[d] = j;
                down[u] = j;
            }
        }

        short l = left[col];
        short r = right[col];
        left[r] = col;
        right[l] = col;
    }

    private static void Search(
        Span<short> left,
        Span<short> right,
        Span<short> up,
        Span<short> down,
        Span<short> size,
        Span<int> currentSolution,
        int depth,
        int maxSolutions,
        ref int solutionCount,
        Span<int> firstSolutionRows)
    {
        short firstCol = right[HeaderNode];
        if (firstCol == HeaderNode)
        {
            if (solutionCount == 0)
            {
                currentSolution[..depth].CopyTo(firstSolutionRows);
            }
            solutionCount++;
            return;
        }

        // Choose the column with the smallest size (S-Heuristic) with early exit on Size <= 1
        short c = firstCol;
        short minSize = size[c];
        if (minSize > 1)
        {
            for (short col = right[c]; col != HeaderNode; col = right[col])
            {
                short colSize = size[col];
                if (colSize < minSize)
                {
                    c = col;
                    minSize = colSize;
                    if (minSize <= 1)
                    {
                        break;
                    }
                }
            }
        }

        if (minSize == 0)
        {
            return;
        }

        Cover(c, left, right, up, down, size);

        for (short r = down[c]; r != c; r = down[r])
        {
            currentSolution[depth] = TemplateRowId[r];

            for (short j = right[r]; j != r; j = right[j])
            {
                Cover(TemplateCol[j], left, right, up, down, size);
            }

            Search(left, right, up, down, size, currentSolution, depth + 1, maxSolutions, ref solutionCount, firstSolutionRows);

            for (short j = left[r]; j != r; j = left[j])
            {
                Uncover(TemplateCol[j], left, right, up, down, size);
            }

            if (solutionCount >= maxSolutions)
            {
                break;
            }
        }

        Uncover(c, left, right, up, down, size);
    }

    /// <summary>
    /// Copies the precomputed 729x324 toroidal matrix template into the provided spans.
    /// </summary>
    public static void CopyTemplateTo(
        Span<short> left,
        Span<short> right,
        Span<short> up,
        Span<short> down,
        Span<short> size)
    {
        TemplateLeft.AsSpan().CopyTo(left);
        TemplateRight.AsSpan().CopyTo(right);
        TemplateUp.AsSpan().CopyTo(up);
        TemplateDown.AsSpan().CopyTo(down);
        TemplateSize.AsSpan().CopyTo(size);
    }

    private static string ConvertSolutionRowsToString(ReadOnlySpan<int> rowIds)
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
