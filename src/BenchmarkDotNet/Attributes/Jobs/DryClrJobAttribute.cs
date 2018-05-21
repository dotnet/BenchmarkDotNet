using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Attributes.Jobs
{
    [PublicAPI]
    public class DryClrJobAttribute : JobConfigBaseAttribute
    {
        [PublicAPI] public DryClrJobAttribute() : base(Job.DryClr) { }

        [PublicAPI] public DryClrJobAttribute(Jit jit, Platform platform) : base(GetJob(jit, platform)) { }

        [PublicAPI] public DryClrJobAttribute(Jit jit) : base(GetJob(jit, null)) { }

        [PublicAPI] public DryClrJobAttribute(Platform platform) : base(GetJob(null, platform)) { }

        [NotNull]
        private static Job GetJob(Jit? jit, Platform? platform)
        {
            var job = Job.DryClr;
            string id = job.Id;

            if (jit.HasValue)
            {
                job = job.With(jit.Value);
                id += "-" + jit.Value;
            }

            if (platform.HasValue)
            {
                job = job.With(platform.Value);
                id += "-" + platform.Value;
            }

            return job.WithId(id);
        }
    }
}