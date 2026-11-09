using SudokuNexus.Application.Abstractions;
using SudokuNexus.Application.Contracts;
using SudokuNexus.Domain.Common;
using SudokuNexus.Domain.Enums;
using SudokuNexus.Domain.Models;
using SudokuNexus.Domain.Primitives;
using SudokuNexus.Engine.ExactCover;
using SudokuNexus.Engine.Generation;
using SudokuNexus.Engine.Grading;

namespace SudokuNexus.Application.Services;

/// <summary>
/// Application-tier orchestrator coordinating Domain models, the stateless Engine (DLX + Logical Grader),
/// the EF Core repository, and the background generation Channel.
/// </summary>
public sealed class PuzzleApplicationService
{
    private readonly DlxSolver _dlxSolver;
    private readonly HumanLogicalGrader _grader;
    private readonly PuzzleGenerator _generator;
    private readonly IPuzzleRepository _repository;
    private readonly IPuzzleGenerationQueue _generationQueue;

    public PuzzleApplicationService(
        DlxSolver dlxSolver,
        HumanLogicalGrader grader,
        PuzzleGenerator generator,
        IPuzzleRepository repository,
        IPuzzleGenerationQueue generationQueue)
    {
        _dlxSolver = dlxSolver;
        _grader = grader;
        _generator = generator;
        _repository = repository;
        _generationQueue = generationQueue;
    }

    /// <summary>
    /// Parses a puzzle string and returns its rich 81-cell state (including candidate pencil-marks).
    /// </summary>
    public Result<BoardStateDto> ParseAndInspect(BoardRequest request)
    {
        Result<Board> parsed = ParseBoardWithEliminations(request);
        if (parsed.IsFailure)
        {
            return Result<BoardStateDto>.Failure(parsed.ErrorCode!, parsed.ErrorMessage!);
        }

        return Result<BoardStateDto>.Success(MapToBoardStateDto(parsed.Value!));
    }

    /// <summary>
    /// Solves a puzzle using Algorithm X (Dancing Links) and verifies solution uniqueness.
    /// </summary>
    public Result<SolveResponseDto> SolveFast(BoardRequest request)
    {
        Result<Board> parsed = ParseBoardWithEliminations(request);
        if (parsed.IsFailure)
        {
            return Result<SolveResponseDto>.Failure(parsed.ErrorCode!, parsed.ErrorMessage!);
        }

        DlxSolveResult dlx = _dlxSolver.Solve(parsed.Value!, checkUniqueness: true);
        BoardStateDto? solvedBoardDto = null;

        if (dlx.IsSolved && dlx.SolvedBoardString is not null)
        {
            Result<Board> solvedBoard = Board.TryParse(dlx.SolvedBoardString);
            if (solvedBoard.IsSuccess)
            {
                solvedBoardDto = MapToBoardStateDto(solvedBoard.Value!);
            }
        }

        return Result<SolveResponseDto>.Success(new SolveResponseDto(
            dlx.IsSolved,
            dlx.HasUniqueSolution,
            dlx.SolutionCount,
            dlx.SolvedBoardString,
            Math.Round(dlx.ElapsedMicroseconds, 2),
            solvedBoardDto));
    }

    /// <summary>
    /// Computes the next human-understandable logical deduction step for the current board state.
    /// </summary>
    public Result<HintResponseDto> GetNextHint(BoardRequest request)
    {
        Result<Board> parsed = ParseBoardWithEliminations(request);
        if (parsed.IsFailure)
        {
            return Result<HintResponseDto>.Failure(parsed.ErrorCode!, parsed.ErrorMessage!);
        }

        Board board = parsed.Value!;
        SolveStep? nextStep = _grader.GetNextHint(board);

        return Result<HintResponseDto>.Success(new HintResponseDto(
            HasHint: nextStep is not null,
            Step: nextStep,
            CurrentBoard: MapToBoardStateDto(board)));
    }

    /// <summary>
    /// Performs a comprehensive analysis combining the microsecond DLX Exact Cover check
    /// with the full step-by-step HumanLogicalGrader walkthrough.
    /// </summary>
    public Result<AnalyzeResponseDto> AnalyzePuzzle(BoardRequest request)
    {
        Result<Board> parsed = ParseBoardWithEliminations(request);
        if (parsed.IsFailure)
        {
            return Result<AnalyzeResponseDto>.Failure(parsed.ErrorCode!, parsed.ErrorMessage!);
        }

        Board board = parsed.Value!;
        DlxSolveResult dlx = _dlxSolver.Solve(board, checkUniqueness: true);
        GradingReport grading = _grader.Grade(board);

        var dlxSummary = new DlxSolveSummaryDto(
            dlx.IsSolved,
            dlx.HasUniqueSolution,
            dlx.SolvedBoardString,
            Math.Round(dlx.ElapsedMicroseconds, 2));

        return Result<AnalyzeResponseDto>.Success(new AnalyzeResponseDto(
            dlxSummary,
            grading,
            MapToBoardStateDto(board)));
    }

    /// <summary>
    /// Retrieves a pre-generated puzzle from the SQLite pool (and signals the background channel to replenish),
    /// or generates one on-the-fly if the pool is empty.
    /// </summary>
    public async Task<StoredPuzzleDto> GetOrGeneratePuzzleAsync(
        DifficultyRating difficulty,
        CancellationToken cancellationToken = default)
    {
        StoredPuzzleDto? cached = await _repository.GetRandomByDifficultyAsync(difficulty, cancellationToken);
        await _generationQueue.EnqueueAsync(difficulty, cancellationToken);

        if (cached is not null)
        {
            return cached;
        }

        GeneratedPuzzle generated = _generator.Generate(difficulty);
        var dto = new StoredPuzzleDto(
            Guid.NewGuid(),
            generated.PuzzleString,
            generated.SolutionString,
            generated.GradingReport.Rating,
            generated.GradingReport.MaxTechniqueUsed,
            generated.GradingReport.TotalWeightScore,
            generated.GivenClueCount,
            DateTimeOffset.UtcNow);

        await _repository.AddPuzzleAsync(dto, cancellationToken);
        return dto;
    }

    /// <summary>
    /// Curated real-world test boards showcasing specific logical deduction techniques.
    /// </summary>
    public static IReadOnlyList<CuratedPresetDto> GetCuratedPresets() =>
    [
        new(
            "easy-singles",
            "Classic Warmup (Naked Singles)",
            "Solvable entirely using Naked Singles",
            DifficultyRating.Easy,
            "530070000600195000098000060800060003400803001700020006060000280000419005000080079"),
        new(
            "medium-hidden",
            "Hidden Corridor (Hidden Singles)",
            "Requires spotting Hidden Singles in Boxes & Lines",
            DifficultyRating.Medium,
            "000260701680070090190004500820100040004602900050003028009300074040050036703018000"),
        new(
            "hard-pairs",
            "Intersection & Pairs",
            "Tests Pointing Pairs and Naked Pairs candidate elimination",
            DifficultyRating.Hard,
            "000000907000420180000705026100904000050000040000507009920108000034059000507000000"),
        new(
            "expert-xwing",
            "Crosswind (X-Wing Showcase)",
            "Features a clean X-Wing pattern to unlock the board",
            DifficultyRating.Expert,
            "100000569492056108056109240009640801064010000218035604040500016905061402621000005"),
        new(
            "inkala-escargot",
            "AI Escargot (Arto Inkala)",
            "One of the hardest known 9x9 Sudokus — stress-tests DLX Exact Cover",
            DifficultyRating.Extreme,
            "100007090030020008009600500005300900010080002600004000300000010040000007007000300")
    ];

    private static Result<Board> ParseBoardWithEliminations(BoardRequest request)
    {
        Result<Board> parsed = Board.TryParse(request.Puzzle);
        if (parsed.IsFailure)
        {
            return parsed;
        }

        Board board = parsed.Value!;
        if (request.AppliedEliminations is not null)
        {
            foreach (CandidateElimination elim in request.AppliedEliminations)
            {
                if (elim.CellIndex is >= 0 and < SudokuTopology.TotalCells &&
                    elim.Digit is >= 1 and <= 9)
                {
                    board.EliminateCandidate(elim.CellIndex, elim.Digit);
                }
            }
        }

        return Result<Board>.Success(board);
    }

    public static BoardStateDto MapToBoardStateDto(Board board)
    {
        var cells = new List<CellStateDto>(SudokuTopology.TotalCells);
        for (int i = 0; i < SudokuTopology.TotalCells; i++)
        {
            var coord = new CellCoordinate(i);
            int val = board.GetValue(i);
            IReadOnlyList<int> candidates = val == 0
                ? board.GetCandidates(i).ToDigits()
                : [];

            cells.Add(new CellStateDto(
                Index: i,
                Row: coord.Row,
                Column: coord.Column,
                Box: coord.Box,
                Value: val,
                IsGiven: board.IsGiven(i),
                Candidates: candidates));
        }

        return new BoardStateDto(
            CanonicalString: board.ToCanonicalString('.'),
            IsValid: board.IsValid(),
            IsSolved: board.IsSolved(),
            FilledCellCount: board.FilledCellCount,
            Cells: cells);
    }
}
