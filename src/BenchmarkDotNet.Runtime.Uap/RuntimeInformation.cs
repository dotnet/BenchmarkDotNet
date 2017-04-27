using BenchmarkDotNet.Environments;
using AssemblyFileVersionAttribute = System.Reflection.AssemblyFileVersionAttribute;

namespace BenchmarkDotNet
{
    public class RuntimeInformation : Portability.RuntimeInformation
    {
        public override bool IsWindows => true;

        public override bool IsLinux => false;

        public override bool IsMac => false;

        public override string GetProcessorName() => Unknown;

        public override Runtime CurrentRuntime => Runtime.Uap;

        public override string GetRuntimeVersion()
        {
            // System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription throws on UWP release build.
            // https://github.com/dotnet/corefx/issues/16769
            var attr = typeof(object).GetTypeInfo().Assembly.GetCustomAttributes(typeof(AssemblyFileVersionAttribute)).OfType<AssemblyFileVersionAttribute>().FirstOrDefault();
            return $".NET Native {attr.Version}";
        }

        public override bool HasRyuJit => true;

        public override string JitInfo => Unknown;

        public override string GetConfiguration() => Unknown;
    }
}