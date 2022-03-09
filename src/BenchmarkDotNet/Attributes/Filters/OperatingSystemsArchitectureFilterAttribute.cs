using System.Linq;
using BenchmarkDotNet.Filters;
using JetBrains.Annotations;
using System.Runtime.InteropServices;

namespace BenchmarkDotNet.Attributes
{
    [PublicAPI]
    public class OperatingSystemsArchitectureFilterAttribute : FilterConfigBaseAttribute
    {
        // CLS-Compliant Code requires a constructor without an array in the argument list
        public OperatingSystemsArchitectureFilterAttribute() { }

        /// <param name="allowed">if set to true, the architectures are enabled, if set to false, disabled</param>
        /// <param name="architectures">the architecture(s) for which the filter should be applied</param>
        public OperatingSystemsArchitectureFilterAttribute(bool allowed, params Architecture[] architectures)
            : base(new SimpleFilter(_ =>
            {
                return allowed
                    ? architectures.Any(architecture => RuntimeInformation.OSArchitecture == architecture)
                    : architectures.All(architecture => RuntimeInformation.OSArchitecture != architecture);
            }))
        {
        }
    }
}