using SudokuNexus.Domain.Enums;
using SudokuNexus.Domain.Models;
using SudokuNexus.Engine.Grading;
using SudokuNexus.Engine.Grading.Strategies;
using Xunit;

namespace SudokuNexus.UnitTests;

public sealed class LogicalStrategiesTests
{
    [Fact]
    public void NakedSingleStrategy_DetectsCellWithOnlyOneCandidate()
    {
        var board = new Board();
        for (var c = 0; c < 8; c++)
        {
            board.PlaceDigit(c, c + 1, isInitialClue: true);
        }

        var strategy = new NakedSingleStrategy();
        SolveStep? step = strategy.FindNextStep(board);

        Assert.NotNull(step);
        Assert.Equal(TechniqueType.NakedSingle, step.Technique);
        Assert.Equal(8, step.PlacedCellIndex);
        Assert.Equal(9, step.PlacedDigit);
    }

    [Fact]
    public void HiddenSingleStrategy_DetectsUniqueCandidateInHouse()
    {
        var board = new Board();
        int[] otherBox0Cells = [1, 2, 9, 10, 11, 18, 19, 20];
        foreach (int idx in otherBox0Cells)
        {
            board.EliminateCandidate(idx, 1);
        }

        var strategy = new HiddenSingleStrategy();
        SolveStep? step = strategy.FindNextStep(board);

        Assert.NotNull(step);
        Assert.Equal(TechniqueType.HiddenSingle, step.Technique);
        Assert.Equal(0, step.PlacedCellIndex);
        Assert.Equal(1, step.PlacedDigit);
    }

    [Fact]
    public void HiddenSingleStrategy_IgnoresNonHiddenCandidatesInLastHouse()
    {
        var board = new Board();
        int[] box8Cells = SudokuTopology.BoxIndices[8];
        for (var i = 0; i < 8; i++)
        {
            board.EliminateCandidate(box8Cells[i], 9);
        }

        var strategy = new HiddenSingleStrategy();
        SolveStep? step = strategy.FindNextStep(board);

        Assert.NotNull(step);
        Assert.Equal(TechniqueType.HiddenSingle, step.Technique);
        Assert.Equal(80, step.PlacedCellIndex);
        Assert.Equal(9, step.PlacedDigit);
    }

    [Fact]
    public void NakedPairStrategy_EliminatesPairDigitsFromOtherCellsInRow()
    {
        var board = new Board();
        for (var d = 1; d <= 9; d++)
        {
            if (d is not 4 and not 7)
            {
                board.EliminateCandidate(0, d);
                board.EliminateCandidate(1, d);
            }
        }

        var strategy = new NakedPairStrategy();
        SolveStep? step = strategy.FindNextStep(board);

        Assert.NotNull(step);
        Assert.Equal(TechniqueType.NakedPair, step.Technique);
        Assert.Null(step.PlacedCellIndex);
        Assert.NotEmpty(step.Eliminations);
        Assert.All(step.Eliminations, e => Assert.True(e.Digit is 4 or 7));
    }

    [Fact]
    public void XWingStrategy_DetectsRowXWingAndEliminatesFromCoverColumns()
    {
        var board = new Board();
        const int targetDigit = 5;

        foreach (int r in new[] { 1, 4 })
        {
            for (var c = 0; c < 9; c++)
            {
                if (c is not 2 and not 6)
                {
                    board.EliminateCandidate(r * 9 + c, targetDigit);
                }
            }
        }

        var strategy = new XWingStrategy();
        SolveStep? step = strategy.FindNextStep(board);

        Assert.NotNull(step);
        Assert.Equal(TechniqueType.XWing, step.Technique);
        Assert.Equal(4, step.PrimaryCells.Count);
        Assert.NotEmpty(step.Eliminations);
        Assert.All(step.Eliminations, e => Assert.Equal(targetDigit, e.Digit));
    }

    [Fact]
    public void HumanLogicalGrader_SolvesEasyAndMediumPuzzlesCompletely()
    {
        const string easyPuzzle =
            "530070000600195000098000060800060003400803001700020006060000280000419005000080079";

        Board board = Board.TryParse(easyPuzzle).Value!;
        var grader = new HumanLogicalGrader(
        [
            new NakedSingleStrategy(),
            new HiddenSingleStrategy(),
            new PointingPairStrategy(),
            new NakedPairStrategy(),
            new XWingStrategy()
        ]);

        GradingReport report = grader.Grade(board);

        Assert.True(report.SolvedEntirelyByLogic);
        Assert.NotEmpty(report.Steps);
        Assert.True(report.Rating is DifficultyRating.Easy or DifficultyRating.Medium);
    }
}
