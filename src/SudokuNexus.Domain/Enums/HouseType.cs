using System.Text.Json.Serialization;

namespace SudokuNexus.Domain.Enums;

/// <summary>
/// Represents the three types of constraint regions ("Houses" / "Units") in a 9x9 Sudoku.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum HouseType
{
    Row = 0,
    Column = 1,
    Box = 2
}
