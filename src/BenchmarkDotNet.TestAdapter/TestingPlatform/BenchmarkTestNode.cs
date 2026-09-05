using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Running;
using Microsoft.Testing.Platform.Extensions.Messages;
using System.Reflection;
using System.Text;

namespace BenchmarkDotNet.TestAdapter.TestingPlatform
{
    /// <summary>
    /// The Microsoft.Testing.Platform view of a single <see cref="BenchmarkCase"/>.
    /// </summary>
    /// <remarks>
    /// A <see cref="TestNode"/> carries mutable state (the property bag holds the current outcome), so a fresh node is
    /// created for every message published on the bus. This class holds the parts that never change.
    /// </remarks>
    internal sealed class BenchmarkTestNode
    {
        private readonly IProperty[] staticProperties;

        private BenchmarkTestNode(BenchmarkCase benchmarkCase, string uid, string displayName, string path, IProperty[] staticProperties)
        {
            BenchmarkCase = benchmarkCase;
            Uid = uid;
            DisplayName = displayName;
            Path = path;
            this.staticProperties = staticProperties;
        }

        /// <summary>
        /// Gets the benchmark this node represents.
        /// </summary>
        public BenchmarkCase BenchmarkCase { get; }

        /// <summary>
        /// Gets the stable identifier of the node. It has to be identical in the discovery and the execution phase,
        /// which may happen in different processes.
        /// </summary>
        public string Uid { get; }

        /// <summary>
        /// Gets the name shown by test runners.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Gets the '/' separated path used by <see cref="Microsoft.Testing.Platform.Requests.TreeNodeFilter"/>.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Creates the node for a benchmark case.
        /// </summary>
        /// <param name="benchmarkCase">The benchmark case to describe.</param>
        /// <param name="includeJobInName">
        /// Whether the display name should be suffixed with the job, which is only useful when the benchmark runs
        /// under more than one job.
        /// </param>
        /// <returns>The created node.</returns>
        public static BenchmarkTestNode Create(BenchmarkCase benchmarkCase, bool includeJobInName)
        {
            var benchmarkMethod = benchmarkCase.Descriptor.WorkloadMethod;
            var type = benchmarkCase.Descriptor.Type;
            var fullClassName = type.GetCorrectCSharpTypeName(prefixWithGlobal: false);
            var parametrizedMethodName = FullNameProvider.GetMethodName(benchmarkCase);
            var jobDisplayInfo = benchmarkCase.GetUnrandomizedJobDisplayInfo();

            // The uid is the hash BenchmarkDotNet itself uses (and reports through `--list json`), so that a benchmark
            // keeps the same identity across processes and across tools. The job is only part of the display name
            // when it actually adds information.
            var uid = benchmarkCase.GetUniqueId();

            // Microsoft.Testing.Platform keeps the display name and the identity apart, so the name is free to be the
            // [Benchmark(Description = ...)] the author chose. GetMethodDisplayName falls back to the method name when
            // no description is set. The path keeps the method name, so that a filter still matches what
            // BenchmarkDotNet's own --filter matches.
            var displayMethodName = FullNameProvider.GetMethodDisplayName(benchmarkCase);
            var displayName = $"{fullClassName}.{displayMethodName}" + (includeJobInName ? $" [{jobDisplayInfo}]" : "");

            var properties = new List<IProperty>
            {
                new TestMethodIdentifierProperty(
                    type.Assembly.FullName,
                    type.Namespace ?? string.Empty,
                    type.GetCorrectCSharpTypeName(prefixWithGlobal: false, includeNamespace: false),
                    benchmarkMethod.Name,
                    benchmarkMethod.IsGenericMethodDefinition ? benchmarkMethod.GetGenericArguments().Length : 0,
                    benchmarkMethod.GetParameters().Select(p => p.ParameterType.FullName ?? p.ParameterType.Name).ToArray(),
                    benchmarkMethod.ReturnType.FullName ?? benchmarkMethod.ReturnType.Name),
            };

            var benchmarkAttribute = benchmarkMethod.ResolveAttribute<BenchmarkAttribute>();
            if (benchmarkAttribute?.SourceCodeFile != null)
            {
                // BenchmarkAttribute captures the line of the attribute itself, and the platform expects 0-based lines.
                var line = Math.Max(0, benchmarkAttribute.SourceCodeLineNumber - 1);
                var position = new LinePosition(line, 0);
                properties.Add(new TestFileLocationProperty(benchmarkAttribute.SourceCodeFile, new LinePositionSpan(position, position)));
            }

            // The categories come from the descriptor rather than from DefaultCategoryDiscoverer, because
            // BenchmarkConverter has already resolved them through the config's ICategoryDiscoverer. Rediscovering
            // them here would hide the categories of a custom discoverer from --treenode-filter, even though
            // BenchmarkDotNet's own --anyCategories and the summary do see them.
            foreach (var category in benchmarkCase.Descriptor.Categories)
                properties.Add(new TestMetadataProperty("Category", category));

            var path = BuildPath(type.Assembly, type.Namespace, fullClassName, parametrizedMethodName, jobDisplayInfo);

            return new BenchmarkTestNode(benchmarkCase, uid, displayName, path, properties.ToArray());
        }

        /// <summary>
        /// Creates a message-bus ready node in the given state.
        /// </summary>
        /// <param name="state">The state of the benchmark, e.g. discovered, passed or failed.</param>
        /// <param name="extraProperties">Any additional properties, such as timing or captured output.</param>
        /// <returns>The created test node.</returns>
        public TestNode ToTestNode(TestNodeStateProperty state, params IProperty[] extraProperties)
        {
            var properties = new PropertyBag(staticProperties);
            properties.Add(state);
            foreach (var property in extraProperties)
                properties.Add(property);

            return new TestNode
            {
                Uid = new TestNodeUid(Uid),
                DisplayName = DisplayName,
                Properties = properties
            };
        }

        /// <summary>
        /// Gets the properties a <see cref="Microsoft.Testing.Platform.Requests.TreeNodeFilter"/> can match against,
        /// which is what makes `--treenode-filter "/*/*/*/*[Category=Fast]"` work.
        /// </summary>
        /// <returns>The filterable properties.</returns>
        public PropertyBag GetFilterableProperties() => new PropertyBag(staticProperties);

        private static string BuildPath(Assembly assembly, string? @namespace, string fullClassName, string methodName, string jobDisplayInfo)
        {
            // The convention followed by the other test frameworks is /<assembly>/<namespace>/<class>/<test>.
            var className = @namespace == null || !fullClassName.StartsWith(@namespace + ".", StringComparison.Ordinal)
                ? fullClassName
                : fullClassName.Substring(@namespace.Length + 1);

            return new StringBuilder()
                .Append('/').Append(Escape(assembly.GetName().Name))
                .Append('/').Append(Escape(@namespace ?? string.Empty))
                .Append('/').Append(Escape(className))
                .Append('/').Append(Escape($"{methodName} [{jobDisplayInfo}]"))
                .ToString();
        }

        // Benchmark parameters are stringified user values, so they can contain the path separator. A '/' cannot be
        // escaped into a segment: Microsoft.Testing.Platform splits the path on every '/' without ever unescaping it,
        // and TreeNodeFilter rejects a filter whose segment contains one, so a raw '/' would both deepen the tree and
        // leave the benchmark unmatchable. Percent encoding keeps the path four levels deep and the segment
        // addressable, at the price of a filter having to spell the separator as '%2F'.
        private static string Escape(string segment) => segment.Replace("%", "%25").Replace("/", "%2F");
    }
}
