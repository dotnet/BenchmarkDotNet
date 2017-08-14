namespace BenchmarkDotNet.Portability
{
    public class VirtualBox : VirtualMachineHypervisor
    {
        public override string Name => "VirtualBox";

        public override bool IsVirtualMachine(string manufacturer, string model)
        {
            return model != null && model.ContainsWithIgnoreCase("virtualbox");
        }
    }
}