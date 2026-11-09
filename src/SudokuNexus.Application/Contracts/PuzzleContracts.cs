using SudokuNexus.Domain.Enums;
using SudokuNexus.Domain.Models;

namespace SudokuNexus.Application.Contracts;

public sealed record BoardRequest(
    string Puzzle,
    IReadOnlyList<CandidateElimination>? AppliedEliminations = null
);

public sealed record CellStateDto(
    int Index,
    int Row,
    int Column,
    int Box,
    int Value,
    bool IsGiven,
    IReadOnlyList<int> Candidates
);

public sealed record BoardStateDto(
    string CanonicalString,
    bool IsValid,
    bool IsSolved,
    int FilledCellCount,
    IReadOnlyList<CellStateDto> Cells
);

public sealed record SolveResponseDto(
    bool IsSolved,
    bool HasUniqueSolution,
    int SolutionCount,
    string? SolvedBoardString,
    double ElapsedMicroseconds,
    BoardStateDto? SolvedBoard
);

public sealed record HintResponseDto(
    bool HasHint,
    SolveStep? Step,
    BoardStateDto CurrentBoard
);

public sealed record AnalyzeResponseDto(
    DlxSolveSummaryDto DlxResult,
    GradingReport Grading,
    BoardStateDto InitialBoard
);

public sealed record DlxSolveSummaryDto(
    bool IsSolvable,
    bool HasUniqueSolution,
    string? SolutionString,
    double ElapsedMicroseconds
);

public sealed record CuratedPresetDto(
    string Id,
    string Name,
    string Subtitle,
    DifficultyRating ExpectedDifficulty,
    string PuzzleString
);
