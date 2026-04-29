using System.Reflection;
using System.Text;

namespace BenchmarkDotNet.Helpers;

internal static class ResourceHelper
{
    internal static ValueTask<string> LoadTemplateAsync(string name, CancellationToken cancellationToken)
        => LoadResourceAsync("BenchmarkDotNet.Templates." + name, cancellationToken);

    private static async ValueTask<string> LoadResourceAsync(string resourceName, CancellationToken cancellationToken)
    {
        using var stream = GetResourceStream(resourceName);

        if (stream == null)
            throw new Exception($"Resource {resourceName} not found");

        using var reader = new CancelableStreamReader(stream, Encoding.UTF8, true);
        return await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
    }

    internal static string LoadTemplate(string name)
        => LoadResource("BenchmarkDotNet.Templates." + name);

    private static string LoadResource(string resourceName)
    {
        using var stream = GetResourceStream(resourceName);

        if (stream == null)
            throw new Exception($"Resource {resourceName} not found");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static Stream GetResourceStream(string resourceName)
        => typeof(ResourceHelper).GetTypeInfo().Assembly.GetManifestResourceStream(resourceName)!;
}