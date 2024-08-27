using Perfolizer.Metrology;
using Perfolizer.Phd.Base;

namespace BenchmarkDotNet.Tests.Phd.Infra;

public static class PhdTestExtensions
{
    public static PhdEntry AddMetrics(this PhdEntry entry, params string[] metrics)
    {
        for (int i = 0; i < metrics.Length; i++)
        {
            var measurement = Measurement.Parse(metrics[i]);
            entry.Add(new PhdEntry
            {
                IterationIndex = i,
                Value = measurement.NominalValue,
                Unit = measurement.Unit
            });
        }
        return entry;
    }
}