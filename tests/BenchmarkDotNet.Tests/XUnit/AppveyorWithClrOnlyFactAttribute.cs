using BenchmarkDotNet.Portability;

namespace BenchmarkDotNet.Tests.XUnit
{
    public class AppVeyorWithClrOnlyFactAttribute : AppVeyorOnlyFactAttribute
    {
        private const string Message = "Test requires CLR";

        public override string Skip => base.Skip ?? (RuntimeInformation.IsFullFramework ? null : Message);
    }
}