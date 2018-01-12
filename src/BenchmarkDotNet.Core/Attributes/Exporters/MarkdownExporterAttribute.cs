using BenchmarkDotNet.Exporters;

namespace BenchmarkDotNet.Attributes.Exporters
{
    // TODO: Find a better way to introduce dialects in the attribute
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
        
        public class HtmlReady : ExporterConfigBaseAttribute
        {
            public HtmlReady() : base(MarkdownExporter.HtmlReady)
            {
            }
        }
    }
}