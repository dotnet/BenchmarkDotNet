namespace BenchmarkDotNet.Portability
{
    public class VirtualBox : VirtualMachineHypervisor
    {
        public static VirtualBox Default { get; } = new VirtualBox();

        private VirtualBox() { }

        public override string Name => "VirtualBox";

        public override bool IsVirtualMachine(string manufacturer, string model)
        {
            return ContainsVmIdentifier(model, "virtualbox");
        }
    }
}