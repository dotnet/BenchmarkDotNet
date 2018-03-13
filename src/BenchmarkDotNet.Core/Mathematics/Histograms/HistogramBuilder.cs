using JetBrains.Annotations;

namespace BenchmarkDotNet.Mathematics.Histograms
{
    [PublicAPI]
    public static class HistogramBuilder
    {
        [PublicAPI] public static readonly IHistogramBuilder Simple = new SimpleHistogramBuilder();
        [PublicAPI] public static readonly IHistogramBuilder Adaptive = new AdaptiveHistogramBuilder();

        [PublicAPI] public static readonly IHistogramBuilder[] AllBuilders = { Simple, Adaptive };
    }
}