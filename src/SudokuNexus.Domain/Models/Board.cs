using System.Text;
using SudokuNexus.Domain.Common;
using SudokuNexus.Domain.Enums;
using SudokuNexus.Domain.Primitives;

namespace SudokuNexus.Domain.Models;

/// <summary>
/// Represents a 9x9 Sudoku board, maintaining placed values and candidate bitmasks for all 81 cells.
/// </summary>
public sealed class Board
{
    private readonly int[] _values = new int[SudokuTopology.TotalCells];
    private readonly CandidateMask[] _candidates = new CandidateMask[SudokuTopology.TotalCells];
    private readonly bool[] _isGiven = new bool[SudokuTopology.TotalCells];

    public Board()
    {
        Array.Fill(_candidates, CandidateMask.All);
    }

    private Board(int[] values, CandidateMask[] candidates, bool[] isGiven)
    {
        Array.Copy(values, _values, SudokuTopology.TotalCells);
        Array.Copy(candidates, _candidates, SudokuTopology.TotalCells);
        Array.Copy(isGiven, _isGiven, SudokuTopology.TotalCells);
    }

    /// <summary>
    /// Creates a deep copy of the board.
    /// </summary>
    public Board Clone() => new(_values, _candidates, _isGiven);

    /// <summary>
    /// Gets the placed value (0 if empty, 1..9 if filled) at <paramref name="cellIndex"/> (0..80).
    /// </summary>
    public int GetValue(int cellIndex) => _values[cellIndex];

    /// <summary>
    /// Gets the placed value at (<paramref name="row"/>, <paramref name="col"/>).
    /// </summary>
    public int GetValue(int row, int col) => _values[row * 9 + col];

    /// <summary>
    /// Returns true if the cell was part of the initial puzzle clues.
    /// </summary>
    public bool IsGiven(int cellIndex) => _isGiven[cellIndex];

    /// <summary>
    /// Gets the current <see cref="CandidateMask"/> for <paramref name="cellIndex"/> (0..80).
    /// </summary>
    public CandidateMask GetCandidates(int cellIndex) => _candidates[cellIndex];

    /// <summary>
    /// Removes a single candidate <paramref name="digit"/> (1..9) from <paramref name="cellIndex"/>.
    /// Returns true if the candidate was present and removed; false if it was already absent.
    /// </summary>
    public bool EliminateCandidate(int cellIndex, int digit)
    {
        CandidateMask before = _candidates[cellIndex];
        if (!before.HasCandidate(digit))
        {
            return false;
        }

        _candidates[cellIndex] = before.WithoutCandidate(digit);
        return true;
    }

    /// <summary>
    /// Places <paramref name="digit"/> (1..9) into <paramref name="cellIndex"/> (0..80) and propagates
    /// constraint elimination to all 20 peer cells.
    /// </summary>
    public void PlaceDigit(int cellIndex, int digit, bool isInitialClue = false)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(cellIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(cellIndex, SudokuTopology.TotalCells);
        ArgumentOutOfRangeException.ThrowIfLessThan(digit, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(digit, 9);

        _values[cellIndex] = digit;
        _isGiven[cellIndex] = isInitialClue;
        _candidates[cellIndex] = CandidateMask.Empty;

        foreach (int peer in SudokuTopology.Peers[cellIndex])
        {
            _candidates[peer] = _candidates[peer].WithoutCandidate(digit);
        }
    }

    /// <summary>
    /// Clears a cell (sets its value back to 0) and recomputes all candidate masks across the board.
    /// Used primarily by the puzzle generator when removing clues.
    /// </summary>
    public void ClearCell(int cellIndex)
    {
        _values[cellIndex] = 0;
        _isGiven[cellIndex] = false;
        RecalculateAllCandidates();
    }

    /// <summary>
    /// Recomputes <c>_candidates</c> for all 81 cells from scratch based on the placed <c>_values</c>.
    /// </summary>
    public void RecalculateAllCandidates()
    {
        for (var i = 0; i < SudokuTopology.TotalCells; i++)
        {
            _candidates[i] = _values[i] == 0 ? CandidateMask.All : CandidateMask.Empty;
        }

        for (var i = 0; i < SudokuTopology.TotalCells; i++)
        {
            int val = _values[i];
            if (val != 0)
            {
                foreach (int peer in SudokuTopology.Peers[i])
                {
                    _candidates[peer] = _candidates[peer].WithoutCandidate(val);
                }
            }
        }
    }

    /// <summary>
    /// Checks whether the current board state violates any Sudoku rules:
    /// - No Row, Column, or 3x3 Box contains duplicate non-zero digits.
    /// - No empty cell (value == 0) has 0 remaining candidates (<c>CandidateMask.Empty</c>).
    /// </summary>
    public bool IsValid()
    {
        for (var i = 0; i < SudokuTopology.GridSize; i++)
        {
            if (HasDuplicates(SudokuTopology.RowIndices[i]))
            {
                return false;
            }

            if (HasDuplicates(SudokuTopology.ColumnIndices[i]))
            {
                return false;
            }

            if (HasDuplicates(SudokuTopology.BoxIndices[i]))
            {
                return false;
            }
        }

        for (var i = 0; i < SudokuTopology.TotalCells; i++)
        {
            if (_values[i] == 0 && _candidates[i].Count == 0)
            {
                return false;
            }
        }
        return true;
    }

    private bool HasDuplicates(int[] cellIndices)
    {
        var seen = 0;

        foreach (int cellIndex in cellIndices)
        {
            int val = _values[cellIndex];

            if (val == 0)
            {
                continue;
            }
            int mask = 1 << val;
            
            if ((seen & mask) != 0)
            {
                return true;
            }

            seen |= mask;
        }

        return false;
    }

    /// <summary>
    /// Returns true if every cell has a placed value (1..9) and <see cref="IsValid"/> is true.
    /// </summary>
    public bool IsSolved()
    {
        for (var i = 0; i < SudokuTopology.TotalCells; i++)
        {
            if (_values[i] == 0)
            {
                return false;
            }
        }
        return IsValid();
    }

    /// <summary>
    /// Counts how many cells currently have a placed value (1..9).
    /// </summary>
    public int FilledCellCount
    {
        get
        {
            var count = 0;
            for (var i = 0; i < SudokuTopology.TotalCells; i++)
            {
                if (_values[i] != 0)
                {
                    count++;
                }
            }
            return count;
        }
    }

    /// <summary>
    /// Parses an 81-character puzzle string (where '1'..'9' are clues and '0' or '.' are empty cells).
    /// </summary>
    public static Result<Board> TryParse(string? puzzleString)
    {
        if (string.IsNullOrWhiteSpace(puzzleString))
        {
            return Result<Board>.Failure("INVALID_PUZZLE_EMPTY", "Puzzle string cannot be null or empty.");
        }

        // Strip whitespace/newlines so formatted grids can also be pasted
        var cleaned = new StringBuilder(81);
        foreach (char ch in puzzleString)
        {
            if (!char.IsWhiteSpace(ch))
            {
                cleaned.Append(ch);
            }
        }

        if (cleaned.Length != SudokuTopology.TotalCells)
        {
            return Result<Board>.Failure(
                "INVALID_PUZZLE_LENGTH",
                $"Puzzle string must contain exactly 81 cells, but found {cleaned.Length}.");
        }

        var board = new Board();
        for (var i = 0; i < SudokuTopology.TotalCells; i++)
        {
            char c = cleaned[i];
            if (c is '0' or '.')
            {
                continue;
            }

            if (c is >= '1' and <= '9')
            {
                int digit = c - '0';
                // Check if placing this clue immediately conflicts with an already-placed peer clue
                foreach (int peer in SudokuTopology.Peers[i])
                {
                    if (board._values[peer] == digit)
                    {
                        var coord1 = new CellCoordinate(i);
                        var coord2 = new CellCoordinate(peer);
                        return Result<Board>.Failure(
                            "CONTRADICTORY_CLUES",
                            $"Initial clue {digit} at {coord1} conflicts with peer at {coord2}.");
                    }
                }

                board.PlaceDigit(i, digit, isInitialClue: true);
            }
            else
            {
                return Result<Board>.Failure(
                    "INVALID_CHARACTER",
                    $"Invalid character '{c}' at index {i}. Only '1'-'9', '0', or '.' are allowed.");
            }
        }

        return Result<Board>.Success(board);
    }

    /// <summary>
    /// Returns the canonical 81-character string representation (with '.' for empty cells, or '0' if <paramref name="emptyChar"/> is '0').
    /// </summary>
    public string ToCanonicalString(char emptyChar = '.')
    {
        Span<char> buffer = stackalloc char[SudokuTopology.TotalCells];
        for (var i = 0; i < SudokuTopology.TotalCells; i++)
        {
            buffer[i] = _values[i] == 0 ? emptyChar : (char)('0' + _values[i]);
        }
        return new string(buffer);
    }

    public override string ToString() => ToCanonicalString('.');
}
