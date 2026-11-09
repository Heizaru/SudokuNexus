using SudokuNexus.Domain.Enums;

namespace SudokuNexus.Application.Abstractions;

/// <summary>
/// Producer-consumer queue contract backed by <see cref="System.Threading.Channels.Channel{T}"/>.
/// Allows HTTP endpoints to request background replenishment of the puzzle pool without blocking API responses.
/// </summary>
public interface IPuzzleGenerationQueue
{
    ValueTask EnqueueAsync(DifficultyRating targetDifficulty, CancellationToken cancellationToken = default);
    IAsyncEnumerable<DifficultyRating> ReadAllAsync(CancellationToken cancellationToken = default);
}
