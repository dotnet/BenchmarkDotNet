using BenchmarkDotNet.Helpers;
using System.Text;

namespace BenchmarkDotNet.Tests.Helpers;

public class CancelableStreamWriterTests
{
    [Fact]
    public async Task FlushAsyncWritesBufferedTextToStream()
    {
        using var stream = new MemoryStream();
        using var writer = new CancelableStreamWriter(stream);

        await writer.WriteAsync("hello", CancellationToken.None);
        Assert.Equal(0, stream.Length);

        await writer.FlushAsync(CancellationToken.None);
        Assert.Equal("hello", Encoding.UTF8.GetString(stream.ToArray()));
    }

    [Fact]
    public async Task FlushAsyncHonorsCanceledToken()
    {
        using var stream = new MemoryStream();
        using var writer = new CancelableStreamWriter(stream);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await writer.WriteAsync("hello", CancellationToken.None);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => writer.FlushAsync(cts.Token));
    }
}
