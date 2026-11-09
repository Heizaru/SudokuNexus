using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using SudokuNexus.Domain.Models;
using SudokuNexus.Engine.ExactCover;
using SudokuNexus.Engine.Grading;
using SudokuNexus.Engine.Grading.Strategies;

BenchmarkSwitcher.FromAssembly(typeof(SolverBenchmarks).Assembly).Run(args);

[MemoryDiagnoser]
public class SolverBenchmarks
{
    private const string EasyPuzzle =
        "530070000600195000098000060800060003400803001700020006060000280000419005000080079";

    private const string AiEscargot =
        "100007090030020008009600500005300900010080002600004000300000010040000007007000300";

    private Board _easyBoard = null!;
    private Board _escargotBoard = null!;
    private DlxSolver _dlxSolver = null!;
    private HumanLogicalGrader _grader = null!;

    [GlobalSetup]
    public void Setup()
    {
        _easyBoard = Board.TryParse(EasyPuzzle).Value!;
        _escargotBoard = Board.TryParse(AiEscargot).Value!;
        _dlxSolver = new DlxSolver();
        _grader = new HumanLogicalGrader(
        [
            new NakedSingleStrategy(),
            new HiddenSingleStrategy(),
            new PointingPairStrategy(),
            new NakedPairStrategy(),
            new XWingStrategy()
        ]);
    }

    [Benchmark(Description = "DLX Solve + Uniqueness (Easy Board)")]
    public DlxSolveResult DlxSolve_Easy() => _dlxSolver.Solve(_easyBoard, checkUniqueness: true);

    [Benchmark(Description = "DLX Solve + Uniqueness (AI Escargot Hardest)")]
    public DlxSolveResult DlxSolve_AiEscargot() => _dlxSolver.Solve(_escargotBoard, checkUniqueness: true);

    [Benchmark(Description = "Human Logical Grader (Step-by-Step Trace)")]
    public GradingReport HumanGrader_Easy() => _grader.Grade(_easyBoard);
}
