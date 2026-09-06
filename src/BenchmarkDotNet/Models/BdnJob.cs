using Perfolizer.Models;

namespace BenchmarkDotNet.Models;

// Extends the perfonar JobInfo (which only exposes Environment and Execution) with an Infrastructure section,
// mirroring how the runtime and toolchain live in Job.Infrastructure.
internal class BdnJob : JobInfo
{
    public BdnInfrastructure? Infrastructure { get; set; }
}
