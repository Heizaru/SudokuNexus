using Microsoft.Extensions.DependencyInjection;
using SudokuNexus.Engine.ExactCover;
using SudokuNexus.Engine.Generation;
using SudokuNexus.Engine.Grading;
using SudokuNexus.Engine.Grading.Strategies;

namespace SudokuNexus.Engine;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the stateless algorithmic solvers, all <see cref="IDeductionStrategy"/> implementations,
    /// the <see cref="HumanLogicalGrader"/>, and the <see cref="PuzzleGenerator"/>.
    /// </summary>
    public static IServiceCollection AddSudokuEngine(this IServiceCollection services)
    {
        services.AddSingleton<DlxSolver>();

        // Register all human deduction strategies (Open-Closed Principle)
        services.AddSingleton<IDeductionStrategy, NakedSingleStrategy>();
        services.AddSingleton<IDeductionStrategy, HiddenSingleStrategy>();
        services.AddSingleton<IDeductionStrategy, PointingPairStrategy>();
        services.AddSingleton<IDeductionStrategy, NakedPairStrategy>();
        services.AddSingleton<IDeductionStrategy, XWingStrategy>();

        services.AddSingleton<HumanLogicalGrader>();
        services.AddSingleton<PuzzleGenerator>();

        return services;
    }
}
