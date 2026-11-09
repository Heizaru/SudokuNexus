using SudokuNexus.Domain.Enums;
using SudokuNexus.Domain.Models;
using SudokuNexus.Engine.Grading.Strategies;

namespace SudokuNexus.Engine.Grading;

/// <summary>
/// MODULE 3.5: The Human Logical Grader Orchestrator.
/// 
/// Uses Constructor Injection (`IEnumerable&lt;IDeductionStrategy&gt;`) to receive all registered
/// solving techniques, orders them from simplest (lowest <see cref="IDeductionStrategy.DifficultyWeight"/>)
/// to hardest, and repeatedly applies the easiest available deduction step until the board is solved
/// or no registered human strategy can make further progress.
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
    /// MODULE 3.5: Simulates a human solving the <paramref name="initialBoard"/> step-by-step,
    /// recording every <see cref="SolveStep"/> and computing the overall <see cref="GradingReport"/>.
    /// </summary>
    public GradingReport Grade(Board initialBoard)
    {
        // TODO [Module 3.5]:
        // 1. Clone `initialBoard` (`Board workingBoard = initialBoard.Clone()`) so we don't mutate the caller's instance.
        // 2. Create a `var steps = new List<SolveStep>()`.
        // 3. While `!workingBoard.IsSolved()`:
        //    - Call `SolveStep? nextStep = GetNextHint(workingBoard)`.
        //    - If `nextStep is null`, break out of the loop (the human strategies got stuck!).
        //    - Apply the step: `nextStep.ApplyTo(workingBoard)` and add `nextStep` to `steps`.
        // 4. Compute:
        //    - `bool solvedEntirelyByLogic = workingBoard.IsSolved()`
        //    - `double maxWeight = steps.Count > 0 ? steps.Max(s => s.DifficultyWeight) : 0.0`
        //    - `double totalScore = Math.Round(steps.Sum(s => s.DifficultyWeight), 2)`
        //    - `TechniqueType maxTechnique = steps.Count > 0 ? steps.MaxBy(s => s.DifficultyWeight)!.Technique : TechniqueType.None`
        //      (If `!solvedEntirelyByLogic`, set `maxTechnique = TechniqueType.BacktrackingFallback`).
        // 5. Map `(solvedEntirelyByLogic, maxWeight)` to a `DifficultyRating` using `ClassifyDifficulty(solvedEntirelyByLogic, maxWeight)`.
        // 6. Return `new GradingReport(...)`.
        throw new NotImplementedException("TODO [Module 3.5]: Implement HumanLogicalGrader.Grade(Board initialBoard).");
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
