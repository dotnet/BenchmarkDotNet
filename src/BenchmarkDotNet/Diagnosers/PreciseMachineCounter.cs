using System.Collections.Generic;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Diagnosers
{
#pragma warning disable CS3001, CS3003 // ulong is non CLS-compliant
    public class PreciseMachineCounter
    {
        [PublicAPI] public int ProfileSourceId { get; }
        [PublicAPI] public string Name { get; }
        [PublicAPI] public HardwareCounter Counter { get; }
        [PublicAPI] public CustomCounter? CustomCounter { get; }
        [PublicAPI] public int Interval { get; }
        [PublicAPI] public Dictionary<ulong, ulong> PerInstructionPointer { get; }

        /// <summary>
        /// Gets the short name for display purposes.
        /// For custom counters, uses the custom short name. For built-in counters, uses the enum's short name.
        /// </summary>
        [PublicAPI] public string ShortName => CustomCounter?.ShortName ?? Counter.ToShortName();

        /// <summary>
        /// Gets whether this counter tracks a metric where higher values are better.
        /// </summary>
        [PublicAPI] public bool HigherIsBetter => CustomCounter?.HigherIsBetter ?? Counter.TheGreaterTheBetter();

        public ulong Count { get; private set; }

        internal PreciseMachineCounter(int profileSourceId, string name, HardwareCounter counter, int interval)
        {
            ProfileSourceId = profileSourceId;
            Name = name;
            Counter = counter;
            CustomCounter = null;
            Interval = interval;
            PerInstructionPointer = new Dictionary<ulong, ulong>(capacity: 10000);
        }

        internal PreciseMachineCounter(int profileSourceId, string name, CustomCounter customCounter, int interval)
        {
            ProfileSourceId = profileSourceId;
            Name = name;
            Counter = HardwareCounter.NotSet;
            CustomCounter = customCounter;
            Interval = interval;
            PerInstructionPointer = new Dictionary<ulong, ulong>(capacity: 10000);
        }

        public void OnSample(ulong instructionPointer)
        {
            checked // if we ever overflow ulong we need to throw!
            {
                Count += (ulong)Interval;

                PerInstructionPointer.TryGetValue(instructionPointer, out ulong currentValue);
                PerInstructionPointer[instructionPointer] = currentValue + (ulong)Interval;
            }
        }
    }
#pragma warning restore CS3001 // Argument type is not CLS-compliant
}