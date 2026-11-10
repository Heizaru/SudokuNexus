using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SudokuNexus.Application.Abstractions;
using SudokuNexus.Application.Contracts;
using SudokuNexus.Application.Services;
using SudokuNexus.Domain.Common;
using SudokuNexus.Domain.Enums;

namespace SudokuNexus.Api.Endpoints;

public static class PuzzleEndpoints
{
    public static IEndpointRouteBuilder MapPuzzleEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/puzzles")
            .WithTags("Sudoku Engine & Grader");

        group.MapGet("/presets", () => TypedResults.Ok(PuzzleApplicationService.GetCuratedPresets()))
            .WithName("GetCuratedPresets")
            .WithSummary("Returns curated test boards (Singles, Pairs, X-Wing, AI Escargot).");

        group.MapPost("/inspect", ([FromBody] BoardRequest request, PuzzleApplicationService service) =>
            ToHttpResult(service.ParseAndInspect(request)))
            .WithName("InspectBoard")
            .WithSummary("Parses an 81-char board string and computes candidate bitmasks for every cell.");

        group.MapPost("/solve", ([FromBody] BoardRequest request, PuzzleApplicationService service) =>
            ToHttpResult(service.SolveFast(request)))
            .WithName("SolvePuzzleFast")
            .WithSummary("Solves a puzzle using Donald Knuth's Algorithm X / Dancing Links and verifies uniqueness.");

        group.MapPost("/hint", ([FromBody] BoardRequest request, PuzzleApplicationService service) =>
            ToHttpResult(service.GetNextHint(request)))
            .WithName("GetNextHint")
            .WithSummary("Finds the single easiest human logical deduction step for the current board.");

        group.MapPost("/analyze", ([FromBody] BoardRequest request, PuzzleApplicationService service) =>
            ToHttpResult(service.AnalyzePuzzle(request)))
            .WithName("AnalyzePuzzle")
            .WithSummary("Runs both DLX Exact Cover and the full step-by-step HumanLogicalGrader.");

        group.MapGet("/generate", async (
            [FromQuery] DifficultyRating difficulty,
            PuzzleApplicationService service,
            CancellationToken cancellationToken) =>
        {
            StoredPuzzleDto puzzle = await service.GetOrGeneratePuzzleAsync(difficulty, cancellationToken);
            return TypedResults.Ok(puzzle);
        })
        .WithName("GeneratePuzzle")
        .WithSummary("Fetches or generates a rotationally symmetric puzzle with a unique solution.");

        group.MapGet("/pool-stats", async (
            IPuzzleRepository repository,
            CancellationToken cancellationToken) =>
        {
            IReadOnlyDictionary<DifficultyRating, int> stats = await repository.GetPoolStatisticsAsync(cancellationToken);
            return TypedResults.Ok(stats);
        })
        .WithName("GetPoolStatistics")
        .WithSummary("Returns the count of pre-generated puzzles in the SQLite pool per difficulty rating.");

        return app;
    }

    private static Results<Ok<T>, ProblemHttpResult> ToHttpResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return TypedResults.Ok(result.Value!);
        }

        return TypedResults.Problem(
            title: result.ErrorCode ?? "VALIDATION_ERROR",
            detail: result.ErrorMessage,
            statusCode: StatusCodes.Status400BadRequest);
    }
}
