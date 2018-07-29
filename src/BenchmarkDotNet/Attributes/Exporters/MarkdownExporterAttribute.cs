using BenchmarkDotNet.Exporters;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Attributes
{
    // TODO: Find a better way to introduce dialects in the attribute
    [PublicAPI]
    public class MarkdownExporterAttribute : ExporterConfigBaseAttribute
    {
        public MarkdownExporterAttribute() : base(MarkdownExporter.Default)
        {
        }

        public class Default : ExporterConfigBaseAttribute
        {
            public Default() : base(MarkdownExporter.Default)
            {
            }
        }

        public class GitHub : ExporterConfigBaseAttribute
        {
            public GitHub() : base(MarkdownExporter.GitHub)
            {
            }
        }

        public class StackOverflow : ExporterConfigBaseAttribute
        {
            public StackOverflow() : base(MarkdownExporter.StackOverflow)
            {
            }
        }

        public class Atlassian : ExporterConfigBaseAttribute
        {
            public Atlassian() : base(MarkdownExporter.Atlassian)
            {
            }
        }
    }
}