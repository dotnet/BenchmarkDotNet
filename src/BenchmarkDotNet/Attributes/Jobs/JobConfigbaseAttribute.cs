using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
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
        /// <remarks>
        /// It is a plain settable property rather than an init-only one so that the attribute stays usable from
        /// projects compiled with C# 7.3, the default for net472 and netstandard2.0.
        /// Every named argument is assigned before anything reads <see cref="Config"/>, which is built lazily,
        /// so the two behave identically here.
        /// </remarks>
        [PublicAPI] public string[]? Categories { get; set; }

        // Named attribute properties are assigned after the constructor has run, so the config cannot be built there:
        // Categories would still be empty. It is read long after the attribute is constructed, so building it lazily
        // is enough to see the categories the user has set.
        public IConfig Config => config ??= job == null
            ? ManualConfig.CreateEmpty()
            : ManualConfig.CreateEmpty().AddJob(
                // `Categories = null` is what an attribute argument of an array type is allowed to be, so it must not
                // be dereferenced without a check.
                Categories is not { Length: > 0 } categories ? job : job.WithCategories(categories).Freeze());

        protected static Job GetJob(Job sourceJob, string runtimeMoniker, Jit? jit, Platform? platform)
        {
            var runtime = Runtime.Parse(runtimeMoniker);
            var baseJob = sourceJob.WithRuntime(runtime).WithId($"{sourceJob.Id}-{runtime}");
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