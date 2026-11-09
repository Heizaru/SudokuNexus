using SudokuNexus.Domain.Common;
using SudokuNexus.Domain.Models;
using SudokuNexus.Domain.Primitives;
using Xunit;

namespace SudokuNexus.UnitTests;

/// <summary>
/// Run these tests while working on Module 1:
/// <c>dotnet test --filter "Module=1"</c>
/// </summary>
[Trait("Module", "1")]
public sealed class Module1_BitmaskAndBoardTests
{
    [Fact]
    public void CandidateMask_All_HasCount9AndContainsAllDigits()
    {
        CandidateMask all = CandidateMask.All;
        Assert.Equal(9, all.Count);

        for (int d = 1; d <= 9; d++)
        {
            Assert.True(all.HasCandidate(d));
        }
    }

    [Fact]
    public void CandidateMask_WithAndWithoutCandidate_UpdatesBitsAndCount()
    {
        CandidateMask mask = CandidateMask.Empty
            .WithCandidate(3)
            .WithCandidate(7);

        Assert.Equal(2, mask.Count);
        Assert.True(mask.HasCandidate(3));
        Assert.True(mask.HasCandidate(7));
        Assert.False(mask.HasCandidate(1));

        CandidateMask afterRemoval = mask.WithoutCandidate(3);
        Assert.Equal(1, afterRemoval.Count);
        Assert.False(afterRemoval.HasCandidate(3));
        Assert.Equal(7, afterRemoval.GetSingleDigit());
    }

    [Fact]
    public void CandidateMask_GetSingleDigit_ThrowsWhenCountIsNotOne()
    {
        Assert.Throws<InvalidOperationException>(() => CandidateMask.Empty.GetSingleDigit());
        Assert.Throws<InvalidOperationException>(() => CandidateMask.All.GetSingleDigit());
    }

    [Fact]
    public void Board_PlaceDigit_SetsValueAndEliminatesFromAll20Peers()
    {
        var board = new Board();
        board.PlaceDigit(cellIndex: 0, digit: 5, isInitialClue: true);

        Assert.Equal(5, board.GetValue(0));
        Assert.True(board.IsGiven(0));
        Assert.Equal(0, board.GetCandidates(0).Count);

        foreach (int peer in SudokuTopology.Peers[0])
        {
            Assert.False(board.GetCandidates(peer).HasCandidate(5));
            Assert.Equal(8, board.GetCandidates(peer).Count);
        }
    }

    [Fact]
    public void Board_TryParseAndIsValid_RecognizesValidAndSolvedBoards()
    {
        const string validPuzzle =
            "530070000600195000098000060800060003400803001700020006060000280000419005000080079";
        const string solvedPuzzle =
            "534678912672195348198342567859761423426853791713924856961537284287419635345286179";

        Result<Board> parsed = Board.TryParse(validPuzzle);
        Assert.True(parsed.IsSuccess);
        Assert.True(parsed.Value!.IsValid());
        Assert.False(parsed.Value.IsSolved());
        Assert.Equal(30, parsed.Value.FilledCellCount);

        Result<Board> solved = Board.TryParse(solvedPuzzle);
        Assert.True(solved.IsSuccess);
        Assert.True(solved.Value!.IsValid());
        Assert.True(solved.Value.IsSolved());
    }

    [Fact]
    public void Board_IsValid_ReturnsFalseWhenEmptyCellHasZeroCandidates()
    {
        var board = new Board();
        // Eliminate all 9 candidates from cell 0 while leaving its value as 0
        for (int d = 1; d <= 9; d++)
        {
            board.EliminateCandidate(0, d);
        }

        Assert.False(board.IsValid());
    }
}
