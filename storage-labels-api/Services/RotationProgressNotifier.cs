using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace StorageLabelsApi.Services;

public class RotationProgressNotifier : IRotationProgressNotifier
{
    private readonly ConcurrentDictionary<Guid, Channel<RotationProgress>> _progressChannels = new();

    public async Task StreamProgressAsync(Guid rotationId, Stream responseStream, CancellationToken cancellationToken)
    {
        var channel = _progressChannels.GetOrAdd(rotationId, _ => 
            Channel.CreateUnbounded<RotationProgress>(new UnboundedChannelOptions
            {
                SingleWriter = false,
                SingleReader = true
            }));

        var writer = new StreamWriter(responseStream, Encoding.UTF8) 
        { 
            AutoFlush = true,
            NewLine = "\n"
        };

        try
        {
            // Send initial heartbeat
            await writer.WriteLineAsync(":\n");

            await foreach (var progress in channel.Reader.ReadAllAsync(cancellationToken))
            {
                var json = JsonSerializer.Serialize(progress, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                await writer.WriteLineAsync($"data: {json}\n");
                await writer.WriteLineAsync(); // Empty line to delimit messages

                // If rotation is complete, failed, or cancelled, close the stream
                if (progress.Status != DataLayer.Models.RotationStatus.InProgress)
                {
                    break;
                }
            }
        }
        finally
        {
            // Clean up the channel when the client disconnects
            _progressChannels.TryRemove(rotationId, out _);
        }
    }

    public async Task NotifyProgressAsync(Guid rotationId, RotationProgress progress)
    {
        if (_progressChannels.TryGetValue(rotationId, out var channel))
        {
            await channel.Writer.WriteAsync(progress);

            // Complete the channel if rotation is done
            if (progress.Status != DataLayer.Models.RotationStatus.InProgress)
            {
                channel.Writer.Complete();
            }
        }
    }
}
