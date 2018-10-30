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
        [PublicAPI] public int Interval { get; }
        [PublicAPI] public Dictionary<ulong, ulong> PerInstructionPointer { get; }

        public ulong Count { get; private set; }

        internal PreciseMachineCounter(int profileSourceId, string name, HardwareCounter counter, int interval)
        {
            ProfileSourceId = profileSourceId;
            Name = name;
            Counter = counter;
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