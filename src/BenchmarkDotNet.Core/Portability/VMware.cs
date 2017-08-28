namespace BenchmarkDotNet.Portability
{
    public class VMware : VirtualMachineHypervisor
    {
        public static VMware Default { get; } = new VMware();

        private VMware() { }

        public override string Name => "VMware";

        public override bool IsVirtualMachine(string manufacturer, string model)
        {
            return ContainsVmIdentifier(model, "vmware");
        }
    }
}