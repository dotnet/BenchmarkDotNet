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

        private BenchmarkTestNode(
            BenchmarkCase benchmarkCase,
            string uid,
            string displayName,
            string path,
            string groupUid,
            IProperty[] staticProperties)
        {
            BenchmarkCase = benchmarkCase;
            Uid = uid;
            DisplayName = displayName;
            Path = path;
            GroupUid = groupUid;
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
        /// Gets the uid of the group node this benchmark is reported under, which is the type declaring it.
        /// </summary>
        /// <remarks>
        /// BenchmarkDotNet produces one summary per type, so the type is also the granularity at which a summary
        /// table can be attached to the tree. See <see cref="CreateGroupNode"/>.
        /// </remarks>
        public string GroupUid { get; }

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
            // [Benchmark(Description = ...)] the author chose, spelled the way it was written: Descriptor's
            // WorkloadMethodDisplayInfo is the console table form, which quotes a description containing a space or a
            // bracket so that BenchmarkDotNet's own --filter can delimit it, and an IDE label does not want that. It
            // is still the fallback, so that a hand-built Descriptor with no attribute keeps its name. The path keeps
            // the method name, so that a filter still matches what --filter matches.
            var benchmarkAttribute = benchmarkMethod.ResolveAttribute<BenchmarkAttribute>();
            var benchmarkName = string.IsNullOrEmpty(benchmarkAttribute?.Description)
                ? benchmarkCase.Descriptor.WorkloadMethodDisplayInfo
                : benchmarkAttribute!.Description!;
            var displayMethodName = FullNameProvider.GetMethodDisplayName(benchmarkCase, benchmarkName);
            var displayName = $"{fullClassName}.{displayMethodName}" + (includeJobInName ? $" [{jobDisplayInfo}]" : "");

            var properties = new List<IProperty>
            {
                new TestMethodIdentifierProperty(
                    type.Assembly.FullName,
                    type.Namespace ?? string.Empty,
                    GetEcmaTypeName(type),
                    benchmarkMethod.Name,
                    benchmarkMethod.IsGenericMethodDefinition ? benchmarkMethod.GetGenericArguments().Length : 0,
                    benchmarkMethod.GetParameters().Select(p => p.ParameterType.FullName ?? p.ParameterType.Name).ToArray(),
                    benchmarkMethod.ReturnType.FullName ?? benchmarkMethod.ReturnType.Name),
            };

            // SourceCodeFile is a non-nullable string that a [CallerFilePath] fills in, so it is empty rather than
            // null on an attribute built without caller information - by an analyzer, or by hand. Publishing a
            // location of "" at line 0 would send an IDE to a file that does not exist.
            if (benchmarkAttribute != null && !string.IsNullOrEmpty(benchmarkAttribute.SourceCodeFile))
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

            return new BenchmarkTestNode(benchmarkCase, uid, displayName, path, GetGroupUid(type), properties.ToArray());
        }

        /// <summary>
        /// Gets the uid of the group node the benchmarks of a type are reported under.
        /// </summary>
        /// <param name="type">The type declaring the benchmarks.</param>
        /// <returns>The uid of the group node.</returns>
        /// <remarks>
        /// A benchmark's own uid is the guid BenchmarkDotNet hashes out of its identity, so a type name can never be
        /// mistaken for one. A closed generic type carries its type arguments in the name, which is what keeps the
        /// instantiations of a generic benchmark class apart - BenchmarkDotNet summarises them separately too.
        /// </remarks>
        public static string GetGroupUid(Type type) => type.GetCorrectCSharpTypeName(prefixWithGlobal: false);

        /// <summary>
        /// Creates the node the benchmarks of a type are reported under.
        /// </summary>
        /// <param name="type">The type declaring the benchmarks.</param>
        /// <param name="extraProperties">Any additional properties, such as the summary table.</param>
        /// <returns>The created test node.</returns>
        /// <remarks>
        /// The node deliberately carries no <see cref="TestNodeStateProperty"/>. That is what makes the platform
        /// serialize it as a `group` rather than as an `action`, which in turn keeps it out of the run's pass, fail
        /// and skip counts - a summary is not a test, and reporting it as one would inflate every total by the
        /// number of benchmark types that ran.
        /// </remarks>
        public static TestNode CreateGroupNode(Type type, params IProperty[] extraProperties)
        {
            var properties = new PropertyBag();
            foreach (var property in extraProperties)
                properties.Add(property);

            // The name is also the identity: a type is named the same way wherever the tree is built, so a discovery
            // and the run that follows it agree on the group without having to carry one across the two processes.
            var name = GetGroupUid(type);

            return new TestNode
            {
                Uid = new TestNodeUid(name),
                DisplayName = name,
                Properties = properties
            };
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

        /// <summary>
        /// Gets the name of a type in the form Microsoft.Testing.Platform documents for
        /// <see cref="TestMethodIdentifierProperty"/>, which is the ECMA-335 one rather than the C# one.
        /// </summary>
        /// <remarks>
        /// A generic type is named after its arity, `GenericProbe`1`, and its type arguments are no part of the name -
        /// they belong to the display name, which carries them. A nested type is qualified by its declaring types,
        /// separated by '+'; the namespace is left out, because the property carries it separately.
        /// </remarks>
        /// <param name="type">The type declaring the benchmark.</param>
        /// <returns>The ECMA-335 name of the type.</returns>
        private static string GetEcmaTypeName(Type type)
        {
            // Type.Name is already the arity form, for an open and for a closed generic type alike.
            var name = type.Name;

            for (var declaringType = type.DeclaringType; declaringType != null; declaringType = declaringType.DeclaringType)
                name = declaringType.Name + "+" + name;

            return name;
        }

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

        // Benchmark parameters are stringified user values, and the leaf ends in the job between brackets, so a
        // segment can contain the characters TreeNodeFilter gives a meaning to. None of them can be escaped into a
        // segment: Microsoft.Testing.Platform splits the path on every '/' without ever unescaping it, and
        // TreeNodeFilter rejects a filter whose segment contains one, so a raw '/' would both deepen the tree and
        // leave the benchmark unmatchable; '[' and ']' delimit a property filter, so a filter spelling a leaf out in
        // full would have its ' [Dry]' parsed as one instead of matched. Percent encoding keeps the path four levels
        // deep and every segment addressable, at the price of a filter having to spell those characters as '%2F',
        // '%5B' and '%5D'.
        private static string Escape(string segment)
            => segment.Replace("%", "%25").Replace("/", "%2F").Replace("[", "%5B").Replace("]", "%5D");
    }
}
