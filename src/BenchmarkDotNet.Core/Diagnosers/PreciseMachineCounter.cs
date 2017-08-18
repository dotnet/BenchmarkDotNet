using System.Collections.Generic;

namespace BenchmarkDotNet.Diagnosers
{
#pragma warning disable CS3001, CS3003 // ulong is non CLS-compilant
    public class PreciseMachineCounter
    {
        public int ProfileSourceId { get; }
        public string Name { get; }
        public HardwareCounter Counter { get; }
        public int Interval { get; }
        public Dictionary<ulong, ulong> PerInstructionPointer { get; }

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

                ulong currentValue = 0;
                PerInstructionPointer.TryGetValue(instructionPointer, out currentValue);
                PerInstructionPointer[instructionPointer] = currentValue + (ulong)Interval;
            }
        }
    }
#pragma warning restore CS3001 // Argument type is not CLS-compliant
}