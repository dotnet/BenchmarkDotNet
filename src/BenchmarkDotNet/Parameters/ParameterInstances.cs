using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;

namespace BenchmarkDotNet.Parameters
{
    public class ParameterInstances : IDisposable, IAsyncDisposable
    {
        public static readonly ParameterInstances Empty = new([]);

        public IReadOnlyList<ParameterInstance> Items { get; }
        public int Count => Items.Count;
        public ParameterInstance this[int index] => Items[index];
        public object? this[string name] => Items.FirstOrDefault(item => item.Name == name)?.Value;

        public ParameterInstances(IReadOnlyList<ParameterInstance> items)
        {
            Items = items;
        }

        public ValueTask DisposeAsync() => Items.DisposeAllAsync();

        public void Dispose()
        {
            using var context = BenchmarkSynchronizationContext.CreateAndSetCurrent();
            context.ExecuteUntilComplete(DisposeAsync());
        }

        public string FolderInfo => string.Join("_", Items.Select(p => $"{p.Name}-{p.ToDisplayText()}")).AsValidFileName();

        public string DisplayInfo => Items.Any() ? "[" + string.Join(", ", Items.Select(p => $"{p.Name}={p.ToDisplayText()}")) + "]" : "";

        public string ValueInfo => Items.Any() ? "[" + string.Join(", ", Items.Select(p => $"{p.Name}={p.Value?.ToString() ?? ParameterInstance.NullParameterTextRepresentation}")) + "]" : "";

        public string PrintInfo => field ??= string.Join("&", Items.Select(p => $"{p.Name}={p.ToDisplayText()}"));

        public ParameterInstance GetArgument(string name) => Items.Single(parameter => parameter.IsArgument && parameter.Name == name);
    }
}