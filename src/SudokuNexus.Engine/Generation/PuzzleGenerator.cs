using SudokuNexus.Domain.Enums;
using SudokuNexus.Domain.Models;
using SudokuNexus.Engine.ExactCover;
using SudokuNexus.Engine.Grading;

namespace SudokuNexus.Engine.Generation;

/// <summary>
/// Represents a generated Sudoku puzzle together with its verified unique solution and logical grading report.
/// </summary>
public sealed record GeneratedPuzzle(
    string PuzzleString,
    string SolutionString,
    int GivenClueCount,
    GradingReport GradingReport
);

/// <summary>
/// MODULE 4.1: Symmetric Sudoku Puzzle Generator.
/// 
/// Combines <see cref="DlxSolver"/> (for fast uniqueness verification) with <see cref="HumanLogicalGrader"/>
/// (to ensure the generated puzzle matches the requested human difficulty level).
/// </summary>
public sealed class PuzzleGenerator
{
    private readonly DlxSolver _dlxSolver;
    private readonly HumanLogicalGrader _grader;

    public PuzzleGenerator(DlxSolver dlxSolver, HumanLogicalGrader grader)
    {
        _dlxSolver = dlxSolver;
        _grader = grader;
    }

    /// <summary>
    /// Generates a new valid, uniquely solvable 9x9 Sudoku puzzle with 180-degree rotational symmetry.
    /// </summary>
    public GeneratedPuzzle Generate(DifficultyRating targetDifficulty, Random? random = null)
    {
        random ??= Random.Shared;

        // Target number of remaining given clues based on difficulty
        int targetClues = targetDifficulty switch
        {
            DifficultyRating.Easy => 38,
            DifficultyRating.Medium => 32,
            DifficultyRating.Hard => 28,
            DifficultyRating.Expert => 25,
            _ => 24
        };

        // 1. Create a valid, randomized full 81-cell solved board
        Board fullBoard = GenerateRandomFullBoard(random);
        string solutionString = fullBoard.ToCanonicalString();

        // 2. Dig holes in 180-degree symmetric pairs (cellIndex and 80 - cellIndex)
        Board puzzleBoard = DigSymmetricClues(fullBoard, targetClues, random);

        // 3. Grade the resulting puzzle with the HumanLogicalGrader
        GradingReport report = _grader.Grade(puzzleBoard);

        return new GeneratedPuzzle(
            PuzzleString: puzzleBoard.ToCanonicalString('.'),
            SolutionString: solutionString,
            GivenClueCount: puzzleBoard.FilledCellCount,
            GradingReport: report);
    }

    /// <summary>
    /// MODULE 4.1: Removes clues in 180-degree rotationally symmetric pairs <c>(i, 80 - i)</c>
    /// while preserving a unique solution (<c>_dlxSolver.CountSolutions(board, limit: 2) == 1</c>).
    /// </summary>
    public Board DigSymmetricClues(Board solvedBoard, int targetClues, Random random)
    {
        // TODO [Module 4.1]:
        // 1. Clone `solvedBoard` into `Board working = solvedBoard.Clone()`.
        // 2. Create a list of cell indices `0..40` (representing each symmetric pair `(i, 80 - i)`, where index 40 is the center cell R5C5)
        //    and shuffle it randomly using `random`.
        // 3. Iterate through each `int cellA` in the shuffled indices:
        //    - If `working.FilledCellCount <= targetClues`, break!
        //    - Let `int cellB = 80 - cellA`.
        //    - Save the current digits `int savedA = working.GetValue(cellA)` and `int savedB = working.GetValue(cellB)`.
        //    - Clear `cellA` (and `cellB` if `cellB != cellA`) using `working.ClearCell(...)`.
        //    - Check uniqueness: `if (_dlxSolver.CountSolutions(working, limit: 2) != 1)`:
        //      Removing this pair created multiple solutions! Restore `savedA` and `savedB` via `working.PlaceDigit(..., isInitialClue: true)`.
        // 4. Return `working`.
        throw new NotImplementedException("TODO [Module 4.1]: Implement PuzzleGenerator.DigSymmetricClues with uniqueness verification.");
    }

    /// <summary>
    /// Generates a complete, valid 81-cell Sudoku grid by seeding the 3 independent diagonal 3x3 boxes
    /// (Box 0, Box 4, Box 8) with shuffled digits 1..9 and solving the rest via <see cref="DlxSolver"/>.
    /// </summary>
    public Board GenerateRandomFullBoard(Random random)
    {
        var board = new Board();
        Span<int> digits = [1, 2, 3, 4, 5, 6, 7, 8, 9];

        // Boxes 0 (top-left), 4 (center), and 8 (bottom-right) do not share any rows or columns!
        ReadOnlySpan<int> independentBoxes = [0, 4, 8];
        foreach (int boxIndex in independentBoxes)
        {
            random.Shuffle(digits);
            int[] boxCells = SudokuTopology.BoxIndices[boxIndex];
            for (int i = 0; i < 9; i++)
            {
                board.PlaceDigit(boxCells[i], digits[i], isInitialClue: true);
            }
        }

        DlxSolveResult result = _dlxSolver.Solve(board, checkUniqueness: false);
        if (!result.IsSolved || result.SolvedBoardString is null)
        {
            throw new InvalidOperationException("Failed to generate a full Sudoku board from diagonal seed.");
        }

        return Board.TryParse(result.SolvedBoardString).Value!;
    }
}
