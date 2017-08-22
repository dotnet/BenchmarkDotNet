namespace BenchmarkDotNet.Portability
{
    public class HyperV : VirtualMachineHypervisor
    {
        public static HyperV Default { get; } = new HyperV();

        private HyperV() { }

        public override string Name => "Hyper-V";

        public override bool IsVirtualMachine(string manufacturer, string model)
        {
            return manufacturer != null && model != null &&
                manufacturer.ContainsWithIgnoreCase("microsoft") && model.ContainsWithIgnoreCase("virtual");
        }
    }
}