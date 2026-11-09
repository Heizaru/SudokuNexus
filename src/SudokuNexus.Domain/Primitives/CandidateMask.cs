using System.Numerics;

namespace SudokuNexus.Domain.Primitives;

/// <summary>
/// MODULE 1.1: Candidate Bitmask.
/// 
/// Wraps an unsigned 16-bit integer (<see cref="ushort"/>) where bits 1 through 9 represent
/// whether digits 1 through 9 are still valid candidates for a Sudoku cell.
/// (Bit 0 is unused so that bit positions align 1-to-1 with digits 1..9).
/// 
/// Example:
/// - Digit 1 -> bit 1 (1 &lt;&lt; 1 = 0b0000_0000_0000_0010 = 2)
/// - Digit 9 -> bit 9 (1 &lt;&lt; 9 = 0b0000_0010_0000_0000 = 512)
/// - All digits 1..9 -> 0b0000_0011_1111_1110 = 0x03FE
/// </summary>
public readonly record struct CandidateMask(ushort Bits)
{
    /// <summary>
    /// A mask with no possible candidates (0).
    /// </summary>
    public static readonly CandidateMask Empty = new(0);

    /// <summary>
    /// A mask where all digits 1..9 are candidates (bits 1..9 set = 0x03FE).
    /// </summary>
    public static readonly CandidateMask All = new(0x03FE);

    /// <summary>
    /// Creates a mask representing a single candidate digit (1..9).
    /// </summary>
    public static CandidateMask FromDigit(int digit)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(digit, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(digit, 9);
        return new CandidateMask((ushort)(1 << digit));
    }

    /// <summary>
    /// Creates a mask from multiple digits (e.g., <c>CandidateMask.FromDigits(1, 4, 9)</c>).
    /// </summary>
    public static CandidateMask FromDigits(params ReadOnlySpan<int> digits)
    {
        ushort bits = 0;
        foreach (int digit in digits)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(digit, 1);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(digit, 9);
            bits |= (ushort)(1 << digit);
        }
        return new CandidateMask(bits);
    }

    /// <summary>
    /// Returns the number of remaining candidate digits in this mask.
    /// </summary>
    public int Count
    {
        get
        {
            //throw new NotImplementedException("TODO [Module 1.1]: Implement CandidateMask.Count using BitOperations.PopCount.");
            return BitOperations.PopCount(Bits);
        }
    }

    /// <summary>
    /// Checks whether the specified <paramref name="digit"/> (1..9) is currently set in the mask.
    /// </summary>
    public bool HasCandidate(int digit)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(digit, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(digit, 9);

        return (Bits & (1 << digit)) != 0;
    }

    /// <summary>
    /// Returns a new <see cref="CandidateMask"/> with the specified <paramref name="digit"/> (1..9) added.
    /// </summary>
    public CandidateMask WithCandidate(int digit)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(digit, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(digit, 9);

        return new CandidateMask((ushort)(1 << digit));
    }

    /// <summary>
    /// Returns a new <see cref="CandidateMask"/> with the specified <paramref name="digit"/> (1..9) cleared.
    /// </summary>
    public CandidateMask WithoutCandidate(int digit)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(digit, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(digit, 9);

        return new CandidateMask((ushort)(Bits & ~(1 << digit)));
    }

    /// <summary>
    /// When <see cref="Count"/> is 1 (a Naked Single), returns that single digit (1..9).
    /// Throws <see cref="InvalidOperationException"/> if the mask does not have exactly 1 bit set.
    /// </summary>
    public int GetSingleDigit()
    {
        return Count != 1 ? throw new InvalidOperationException("Mask does not contain exactly one candidate") : BitOperations.TrailingZeroCount(Bits);
    }

    /// <summary>
    /// Enumerates all candidate digits (1..9) currently present in this mask in ascending order.
    /// </summary>
    public IReadOnlyList<int> ToDigits()
    {
        var list = new List<int>(9);
        for (int d = 1; d <= 9; d++)
        {
            if ((Bits & (1 << d)) != 0)
            {
                list.Add(d);
            }
        }
        return list;
    }

    public static CandidateMask operator |(CandidateMask left, CandidateMask right) =>
        new((ushort)(left.Bits | right.Bits));

    public static CandidateMask operator &(CandidateMask left, CandidateMask right) =>
        new((ushort)(left.Bits & right.Bits));

    public static CandidateMask operator ~(CandidateMask mask) =>
        new((ushort)(~mask.Bits & All.Bits));

    public override string ToString() => $"{{{string.Join(",", ToDigits())}}}";
}
