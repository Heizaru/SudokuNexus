using System.Text.Json.Serialization;

namespace SudokuNexus.Domain.Enums;

/// <summary>
/// Classification of a Sudoku puzzle based on the most advanced human logical technique
/// required to solve it (modeled after Sudoku Explainer / SE ratings).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DifficultyRating
{
    Easy = 1,
    Medium = 2,
    Hard = 3,
    Expert = 4,
    Extreme = 5
}
