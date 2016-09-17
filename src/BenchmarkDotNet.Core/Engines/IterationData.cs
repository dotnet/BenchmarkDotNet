namespace BenchmarkDotNet.Engines
{
    public struct IterationData
    {
        public IterationMode IterationMode { get; }
        public int Index { get; }
        public long InvokeCount { get; }

        public IterationData(IterationMode iterationMode, int index, long invokeCount)
        {
            IterationMode = iterationMode;
            Index = index;
            InvokeCount = invokeCount;
        }
    }
}