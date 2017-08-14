namespace BenchmarkDotNet.Portability
{
    public abstract class VirtualMachineHypervisor
    {
        public abstract string Name { get; }

        public abstract bool IsVirtualMachine(string manufacturer, string model);
    }
}