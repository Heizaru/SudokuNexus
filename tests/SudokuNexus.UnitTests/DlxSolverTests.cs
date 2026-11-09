using SudokuNexus.Domain.Models;
using SudokuNexus.Engine.ExactCover;
using Xunit;

namespace SudokuNexus.UnitTests;

public sealed class DlxSolverTests
{
    [Fact]
    public void CoverAndUncover_RestoresExactToroidalMatrixState()
    {
        Span<short> left = stackalloc short[DlxSolver.TotalNodes];
        Span<short> right = stackalloc short[DlxSolver.TotalNodes];
        Span<short> up = stackalloc short[DlxSolver.TotalNodes];
        Span<short> down = stackalloc short[DlxSolver.TotalNodes];
        Span<short> size = stackalloc short[DlxSolver.TotalColumns + 1];

        DlxSolver.CopyTemplateTo(left, right, up, down, size);
        short firstCol = right[DlxSolver.HeaderNode];

        DlxSolver.Cover(firstCol, left, right, up, down, size);
        Assert.NotEqual(firstCol, right[DlxSolver.HeaderNode]);

        DlxSolver.Uncover(firstCol, left, right, up, down, size);
        Assert.Equal(firstCol, right[DlxSolver.HeaderNode]);
        Assert.Equal(9, size[firstCol]);
    }

    [Fact]
    public void Solve_StandardPuzzle_FindsUniqueSolution()
    {
        const string puzzle =
            "530070000600195000098000060800060003400803001700020006060000280000419005000080079";
        const string expectedSolution =
            "534678912672195348198342567859761423426853791713924856961537284287419635345286179";

        Board board = Board.TryParse(puzzle).Value!;
        var solver = new DlxSolver();

        DlxSolveResult result = solver.Solve(board, checkUniqueness: true);

        Assert.True(result.IsSolved);
        Assert.True(result.HasUniqueSolution);
        Assert.Equal(1, result.SolutionCount);
        Assert.Equal(expectedSolution, result.SolvedBoardString);
    }

    [Fact]
    public void Solve_AiEscargotHardestSudoku_SolvesCorrectlyAndUniquely()
    {
        const string aiEscargot =
            "100007090030020008009600500005300900010080002600004000300000010040000007007000300";

        Board board = Board.TryParse(aiEscargot).Value!;
        var solver = new DlxSolver();

        DlxSolveResult result = solver.Solve(board, checkUniqueness: true);

        Assert.True(result.IsSolved);
        Assert.True(result.HasUniqueSolution);
        Assert.NotNull(result.SolvedBoardString);

        Board solvedBoard = Board.TryParse(result.SolvedBoardString).Value!;
        Assert.True(solvedBoard.IsSolved());
    }

    [Fact]
    public void CountSolutions_UnderConstrainedBoard_DetectsMultipleSolutions()
    {
        string almostEmpty = "1" + new string('.', 80);
        Board board = Board.TryParse(almostEmpty).Value!;
        var solver = new DlxSolver();

        int count = DlxSolver.CountSolutions(board, limit: 2);
        Assert.Equal(2, count);
    }
}
