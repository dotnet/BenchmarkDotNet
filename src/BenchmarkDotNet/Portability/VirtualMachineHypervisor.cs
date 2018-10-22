namespace BenchmarkDotNet.Portability
{
    public abstract class VirtualMachineHypervisor
    {
        public abstract string Name { get; }

        public abstract bool IsVirtualMachine(string manufacturer, string model);

        protected static bool ContainsVmIdentifier(string systemInformation, string vmIdentifier)
        {
            return systemInformation != null && systemInformation.ContainsWithIgnoreCase(vmIdentifier);
        }
    }
}