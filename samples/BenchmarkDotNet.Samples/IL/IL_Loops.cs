using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.Samples.IL
{
    public class IL_Loops
    {
        private int[] initialValuesArray;
        private List<int> initialValuesList;

        [GlobalSetup]
        public void GlobalSetupData()
        {
            int MaxCounter = 1000;
            initialValuesArray = Enumerable.Range(0, MaxCounter).ToArray();
            initialValuesList = Enumerable.Range(0, MaxCounter).ToList();
        }

        [Benchmark]
        public int ForLoop()
        {
            var counter = 0;
            for (int i = 0; i < initialValuesArray.Length; i++)
                counter += initialValuesArray[i];
            return counter;
        }

        [Benchmark]
        public int ForEachArray()
        {
            var counter = 0;
            foreach (var i in initialValuesArray)
                counter += i;
            return counter;
        }

        [Benchmark]
        public int ForEachArrayAsIList()
        {
            var counter = 0;
            foreach (var i in (initialValuesArray as IList<int>))
                counter += i;
            return counter;
        }

        [Benchmark]
        public int ForEachList()
        {
            var counter = 0;
            foreach (var i in initialValuesList)
                counter += i;
            return counter;
        }
    }
}
