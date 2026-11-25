using JetBrains.Annotations;
using SudokuNexus.Domain.Enums;
using SudokuNexus.Domain.Models;

namespace SudokuNexus.Application.Contracts;

public sealed record BoardRequest(
    string Puzzle,
    IReadOnlyList<CandidateElimination>? AppliedEliminations = null
);

[UsedImplicitly]
public sealed record CellStateDto(
    int Index,
    int Row,
    int Column,
    int Box,
    int Value,
    bool IsGiven,
    IReadOnlyList<int> Candidates
);

[UsedImplicitly]
public sealed record BoardStateDto(
    string CanonicalString,
    bool IsValid,
    bool IsSolved,
    int FilledCellCount,
    IReadOnlyList<CellStateDto> Cells
);

[UsedImplicitly]
public sealed record SolveResponseDto(
    bool IsSolved,
    bool HasUniqueSolution,
    int SolutionCount,
    string? SolvedBoardString,
    double ElapsedMicroseconds,
    BoardStateDto? SolvedBoard
);

[UsedImplicitly]
public sealed record HintResponseDto(
    bool HasHint,
    SolveStep? Step,
    BoardStateDto CurrentBoard
);

[UsedImplicitly]
public sealed record AnalyzeResponseDto(
    DlxSolveSummaryDto DlxResult,
    GradingReport Grading,
    BoardStateDto InitialBoard
);

[UsedImplicitly]
public sealed record DlxSolveSummaryDto(
    bool IsSolvable,
    bool HasUniqueSolution,
    string? SolutionString,
    double ElapsedMicroseconds
);

[UsedImplicitly]
public sealed record CuratedPresetDto(
    string Id,
    string Name,
    string Subtitle,
    DifficultyRating ExpectedDifficulty,
    string PuzzleString
);
