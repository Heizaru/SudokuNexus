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
/// Symmetric Sudoku Puzzle Generator.
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
    /// Removes clues in 180-degree rotationally symmetric pairs <c>(i, 80 - i)</c>
    /// while preserving a unique solution (<c>_dlxSolver.CountSolutions(board, limit: 2) == 1</c>).
    /// </summary>
    public Board DigSymmetricClues(Board solvedBoard, int targetClues, Random random)
    {
        Board working = solvedBoard.Clone();

        Span<int> pairIndices = stackalloc int[41];
        for (int i = 0; i <= 40; i++)
        {
            pairIndices[i] = i;
        }

        random.Shuffle(pairIndices);

        foreach (int cellA in pairIndices)
        {
            if (working.FilledCellCount <= targetClues)
            {
                break;
            }

            int cellB = 80 - cellA;
            int savedA = working.GetValue(cellA);
            int savedB = working.GetValue(cellB);

            working.ClearCell(cellA);
            if (cellB != cellA)
            {
                working.ClearCell(cellB);
            }

            if (_dlxSolver.CountSolutions(working, limit: 2) != 1)
            {
                working.PlaceDigit(cellA, savedA, isInitialClue: true);
                if (cellB != cellA)
                {
                    working.PlaceDigit(cellB, savedB, isInitialClue: true);
                }
            }
        }

        return working;
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
