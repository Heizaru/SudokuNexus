using System.Threading.Channels;
using SudokuNexus.Application.Abstractions;
using SudokuNexus.Domain.Enums;

namespace SudokuNexus.Infrastructure.Background;

/// <summary>
/// Thread-safe bounded channel wrapper for queuing background puzzle generation requests.
/// </summary>
public sealed class ChannelPuzzleGenerationQueue : IPuzzleGenerationQueue
{
    private readonly Channel<DifficultyRating> _channel;

    public ChannelPuzzleGenerationQueue(int capacity = 32)
    {
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
            SingleWriter = false
        };
        _channel = Channel.CreateBounded<DifficultyRating>(options);
    }

    public ValueTask EnqueueAsync(DifficultyRating targetDifficulty, CancellationToken cancellationToken = default) =>
        _channel.Writer.WriteAsync(targetDifficulty, cancellationToken);

    public IAsyncEnumerable<DifficultyRating> ReadAllAsync(CancellationToken cancellationToken = default) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}
