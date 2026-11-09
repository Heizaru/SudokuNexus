using SudokuNexus.Domain.Enums;
using SudokuNexus.Domain.Models;
using SudokuNexus.Engine.Grading.Strategies;

namespace SudokuNexus.Engine.Grading;

/// <summary>
/// Evaluates and grades Sudoku puzzles by iteratively applying registered <see cref="IDeductionStrategy"/>
/// implementations in order of increasing difficulty weight.
/// </summary>
public sealed class HumanLogicalGrader
{
    private readonly IReadOnlyList<IDeductionStrategy> _orderedStrategies;

    public HumanLogicalGrader(IEnumerable<IDeductionStrategy> strategies)
    {
        _orderedStrategies = strategies
            .OrderBy(s => s.DifficultyWeight)
            .ToList();
    }

    /// <summary>
    /// Finds the single easiest logical deduction step available on <paramref name="board"/>,
    /// or returns <c>null</c> if the board is already solved or requires a technique beyond
    /// the registered strategies.
    /// </summary>
    public SolveStep? GetNextHint(Board board)
    {
        if (board.IsSolved())
        {
            return null;
        }

        foreach (IDeductionStrategy strategy in _orderedStrategies)
        {
            SolveStep? step = strategy.FindNextStep(board);
            if (step is not null)
            {
                return step;
            }
        }

        return null;
    }

    /// <summary>
    /// Simulates a human solving the <paramref name="initialBoard"/> step-by-step,
    /// recording every <see cref="SolveStep"/> and computing the overall <see cref="GradingReport"/>.
    /// </summary>
    public GradingReport Grade(Board initialBoard)
    {
        Board workingBoard = initialBoard.Clone();
        var steps = new List<SolveStep>();

        while (!workingBoard.IsSolved())
        {
            SolveStep? nextStep = GetNextHint(workingBoard);
            if (nextStep is null)
            {
                break;
            }

            nextStep.ApplyTo(workingBoard);
            steps.Add(nextStep);
        }

        bool solvedEntirelyByLogic = workingBoard.IsSolved();
        SolveStep? hardestStep = steps.MaxBy(s => s.DifficultyWeight);
        double maxWeight = hardestStep?.DifficultyWeight ?? 0.0;
        double totalScore = Math.Round(steps.Sum(s => s.DifficultyWeight), 2);
        TechniqueType maxTechnique = !solvedEntirelyByLogic
            ? TechniqueType.BacktrackingFallback
            : hardestStep?.Technique ?? TechniqueType.None;

        DifficultyRating rating = ClassifyDifficulty(solvedEntirelyByLogic, maxWeight);
        return new GradingReport(
            rating,
            maxTechnique,
            maxWeight,
            totalScore,
            solvedEntirelyByLogic,
            workingBoard.ToCanonicalString(),
            steps);
    }

    /// <summary>
    /// Maps the maximum step weight (Sudoku Explainer scale) to a discrete <see cref="DifficultyRating"/>.
    /// </summary>
    public static DifficultyRating ClassifyDifficulty(bool solvedEntirelyByLogic, double maxStepWeight)
    {
        if (!solvedEntirelyByLogic)
        {
            return DifficultyRating.Extreme;
        }

        return maxStepWeight switch
        {
            <= 1.2 => DifficultyRating.Easy,     // Only Naked Singles (1.0)
            <= 2.0 => DifficultyRating.Medium,   // Requires Hidden Singles (1.5)
            <= 3.0 => DifficultyRating.Hard,     // Requires Pointing Pairs (2.6) or Naked Pairs (3.0)
            _ => DifficultyRating.Expert         // Requires X-Wing (3.2+)
        };
    }
}
