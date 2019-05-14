using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Environments;
using System;

namespace BenchmarkDotNet.Jobs
{
    public sealed class PowerPlanMode : JobMode<PowerPlanMode>
    {
        public static readonly Characteristic<PowerPlan> PowerPlanCharacteristic = CreateCharacteristic<PowerPlan>(nameof(PowerPlan));
        public static readonly Characteristic<Guid> PowerPlanGuidCharacteristic = CreateCharacteristic<Guid>(nameof(PowerPlanGuid));

        public PowerPlanMode() : this(null)
        {
        }

        private PowerPlanMode(string id) : base(id)
        {
        }

        public PowerPlan PowerPlan
        {
            get { return PowerPlanCharacteristic[this]; }
            set { PowerPlanCharacteristic[this] = value; }
        }

        public Guid PowerPlanGuid
        {
            get { return PowerPlanGuidCharacteristic[this]; }
            set { PowerPlanGuidCharacteristic[this] = value; }
        }

        public bool Equals(PowerPlanMode other)
           => other != null
               && other.PowerPlan == PowerPlan
               && other.PowerPlanGuid == PowerPlanGuid;
    }
}
