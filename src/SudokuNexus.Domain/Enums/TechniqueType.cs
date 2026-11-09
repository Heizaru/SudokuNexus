using System.Text.Json.Serialization;

namespace SudokuNexus.Domain.Enums;

/// <summary>
/// Deduction techniques supported by the logical grading engine.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TechniqueType
{
    None = 0,
    NakedSingle = 10,
    HiddenSingle = 15,
    PointingPair = 26,
    NakedPair = 30,
    XWing = 32,
    BacktrackingFallback = 90
}
