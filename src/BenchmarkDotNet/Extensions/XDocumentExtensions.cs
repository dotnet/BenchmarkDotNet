#if NETSTANDARD2_0
using System.Xml.Linq;

namespace BenchmarkDotNet.Extensions;

internal static class XDocumentExtensions
{
    /// <summary>
    /// Helper extension method for netstandard2.0.
    /// It provides an API `SaveAsync`, but the actual processing is performed synchronously.
    /// </summary>
    public static async ValueTask SaveAsync(this XDocument doc, Stream stream, SaveOptions options, CancellationToken cancellationToken)
    {
        doc.Save(stream, options);
    }
}
#endif
