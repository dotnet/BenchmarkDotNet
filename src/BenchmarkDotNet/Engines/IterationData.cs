namespace BenchmarkDotNet.Engines
{
    public struct IterationData
    {
        public IterationMode IterationMode { get; }
        public IterationStage IterationStage { get; }
        public int Index { get; }
        public long InvokeCount { get; }
        public int UnrollFactor { get; }

        public IterationData(IterationMode iterationMode, IterationStage iterationStage, int index, long invokeCount, int unrollFactor)
        {
            IterationMode = iterationMode;
            IterationStage = iterationStage;
            Index = index;
            InvokeCount = invokeCount;
            UnrollFactor = unrollFactor;
        }
    }
}