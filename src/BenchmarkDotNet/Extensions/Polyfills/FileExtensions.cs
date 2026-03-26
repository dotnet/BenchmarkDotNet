#if NETSTANDARD2_0
using BenchmarkDotNet.Helpers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO;

internal static class FileExtensions
{
    extension(File)
    {
        public static Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken = default)
            => WriteAllTextAsync(path, contents, Encoding.UTF8, cancellationToken);

        public static async Task WriteAllTextAsync(string path, string contents, Encoding encoding, CancellationToken cancellationToken = default)
        {
            using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, 4096, useAsync: true);
            using var writer = new CancelableStreamWriter(stream, encoding, 4096);
            await writer.WriteAsync(contents.AsMemory(), cancellationToken).ConfigureAwait(false);
            await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        public static Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default)
            => ReadAllTextAsync(path, Encoding.UTF8, cancellationToken);

        public static async Task<string> ReadAllTextAsync(string path, Encoding encoding, CancellationToken cancellationToken = default)
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
            using var reader = new CancelableStreamReader(stream, encoding, detectEncodingFromByteOrderMarks: true, 4096);
            return await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
#endif
