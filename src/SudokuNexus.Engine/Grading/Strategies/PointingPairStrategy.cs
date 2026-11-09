using SudokuNexus.Domain.Enums;
using SudokuNexus.Domain.Models;
using SudokuNexus.Domain.Primitives;

namespace SudokuNexus.Engine.Grading.Strategies;

/// <summary>
/// MODULE 3.3: Pointing Pair / Pointing Triple (Locked Candidates Type 1) Strategy (Weight: 2.6).
/// 
/// Rule:
/// If within a 3x3 Box, all empty cells that can hold candidate digit d (2 or 3 cells)
/// lie in the SAME Row (or the SAME Column), then digit d MUST be placed in one of those
/// cells inside the Box. Therefore, d can be safely ELIMINATED from all other cells in
/// that Row (or Column) outside the Box!
/// 
/// Note: Unlike Naked/Hidden Single, this is a candidate ELIMINATION strategy:
/// <c>PlacedCellIndex</c> and <c>PlacedDigit</c> are <c>null</c>, and <c>Eliminations</c>
/// contains at least one <see cref="CandidateElimination"/>.
/// </summary>
public sealed class PointingPairStrategy : IDeductionStrategy
{
    public TechniqueType Technique => TechniqueType.PointingPair;
    public double DifficultyWeight => 2.6;

    public SolveStep? FindNextStep(Board board)
    {
        for (int boxIndex = 0; boxIndex < 9; boxIndex++)
        {
            ReadOnlySpan<int> boxCells = SudokuTopology.BoxIndices[boxIndex];

            for (int digit = 1; digit <= 9; digit++)
            {
                var candidateCells = new List<int>(3);
                foreach (int cell in boxCells)
                {
                    if (board.GetValue(cell) == 0 && board.GetCandidates(cell).HasCandidate(digit))
                    {
                        candidateCells.Add(cell);
                    }
                }

                if (candidateCells.Count is < 2 or > 3)
                {
                    continue;
                }

                // Check if all candidateCells share the same Row
                int firstRow = new CellCoordinate(candidateCells[0]).Row;
                if (candidateCells.All(c => new CellCoordinate(c).Row == firstRow))
                {
                    var eliminations = FindLineEliminationsOutsideBox(
                        board, SudokuTopology.RowIndices[firstRow], boxIndex, digit);

                    if (eliminations.Count > 0)
                    {
                        string coords = string.Join(", ", candidateCells.Select(c => new CellCoordinate(c)));
                        return new SolveStep(
                            Technique,
                            DifficultyWeight,
                            $"Pointing Pair/Triple: Digit {digit} in Box {boxIndex + 1} is locked to Row {firstRow + 1} ({coords}), eliminating {digit} from {eliminations.Count} peer cell(s).",
                            candidateCells,
                            PlacedCellIndex: null,
                            PlacedDigit: null,
                            eliminations);
                    }
                }

                // Check if all candidateCells share the same Column
                int firstCol = new CellCoordinate(candidateCells[0]).Column;
                if (candidateCells.All(c => new CellCoordinate(c).Column == firstCol))
                {
                    var eliminations = FindLineEliminationsOutsideBox(
                        board, SudokuTopology.ColumnIndices[firstCol], boxIndex, digit);

                    if (eliminations.Count > 0)
                    {
                        string coords = string.Join(", ", candidateCells.Select(c => new CellCoordinate(c)));
                        return new SolveStep(
                            Technique,
                            DifficultyWeight,
                            $"Pointing Pair/Triple: Digit {digit} in Box {boxIndex + 1} is locked to Column {firstCol + 1} ({coords}), eliminating {digit} from {eliminations.Count} peer cell(s).",
                            candidateCells,
                            PlacedCellIndex: null,
                            PlacedDigit: null,
                            eliminations);
                    }
                }
            }
        }

        return null;
    }

    private static List<CandidateElimination> FindLineEliminationsOutsideBox(
        Board board,
        ReadOnlySpan<int> lineCells,
        int boxIndex,
        int digit)
    {
        var eliminations = new List<CandidateElimination>();
        foreach (int cell in lineCells)
        {
            var coord = new CellCoordinate(cell);
            if (coord.Box != boxIndex &&
                board.GetValue(cell) == 0 &&
                board.GetCandidates(cell).HasCandidate(digit))
            {
                eliminations.Add(new CandidateElimination(cell, digit));
            }
        }
        return eliminations;
    }
}
