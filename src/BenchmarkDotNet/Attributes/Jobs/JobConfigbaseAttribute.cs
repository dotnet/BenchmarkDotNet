using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly)]
    public class JobConfigBaseAttribute : Attribute, IConfigSource
    {
        private readonly Job? job;
        private IConfig? config;

        // CLS-Compliant Code requires a constructor which use only CLS-compliant types
        [PublicAPI]
        public JobConfigBaseAttribute() { }

        protected JobConfigBaseAttribute(Job job) => this.job = job;

        /// <summary>
        /// The categories of the job defined by this attribute. Categories are metadata used to select which jobs are
        /// executed (see <see cref="BenchmarkDotNet.Filters.JobCategoryFilter"/>), they don't affect how the job is executed.
        /// </summary>
        [PublicAPI] public string[] Categories { get; set; } = [];

        // Named attribute properties are assigned after the constructor has run, so the config cannot be built there:
        // Categories would still be empty. It is read long after the attribute is constructed, so building it lazily
        // is enough to see the categories the user has set.
        public IConfig Config => config ??= job == null
            ? ManualConfig.CreateEmpty()
            : ManualConfig.CreateEmpty().AddJob(Categories.Length == 0 ? job : job.WithCategories(Categories).Freeze());

        protected static Job GetJob(Job sourceJob, RuntimeMoniker runtimeMoniker, Jit? jit, Platform? platform)
        {
            var runtime = runtimeMoniker.GetRuntime();
            var baseJob = sourceJob.WithRuntime(runtime).WithId($"{sourceJob.Id}-{runtime.Name}");
            var id = baseJob.Id;

            if (jit.HasValue)
            {
                baseJob = baseJob.WithJit(jit.Value);
                id += "-" + jit.Value;
            }

            if (platform.HasValue)
            {
                baseJob = baseJob.WithPlatform(platform.Value);
                id += "-" + platform.Value;
            }

            return baseJob.WithId(id).Freeze();
        }
    }
}