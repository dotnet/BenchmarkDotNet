namespace BenchmarkDotNet.Portability
{
    public class VMware : VirtualMachineHypervisor
    {
        public override string Name => "VMware";

        public override bool IsVirtualMachine(string manufacturer, string model)
        {
            return model != null && model.ContainsWithIgnoreCase("vmware");
        }
    }
}