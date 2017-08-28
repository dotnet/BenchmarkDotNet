namespace BenchmarkDotNet.Tests.XUnit
{
    public class AppVeyorWithClassicFrameworkOnlyFactAttribute : AppVeyorOnlyFactAttribute
    {
        private const string Message = "Test requires classic .NET Framework";

        private bool classicDotNet
        {
            get
            {
#if !CORE
                return true;
#else
                return false;
#endif
            }
        }

        public override string Skip => base.Skip ?? (classicDotNet ? null : Message);
    }
}