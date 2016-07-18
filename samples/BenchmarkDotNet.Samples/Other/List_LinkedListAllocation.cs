using System.Collections.Generic;
using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.Samples.Other
{
    /// <summary>
    /// Example of slow LinkedList insertion and usage of [Cleanup] attribute.
    /// </summary>
    public class List_LinkedListAllocation
    {
        private List<int> list;
        private LinkedList<int> linkedList;

        private const int Total = 10000;

        [Setup]
        public void Setup()
        {
            list = new List<int>();
            linkedList = new LinkedList<int>();
        }

        [Benchmark(Baseline = true)]
        public void ListAllocationTest()
        {
            for (int i = 0; i < Total; i++)
            {
                list.Add(i);
            }
        }

        [Benchmark]
        public void LinkedListAllocationTest()
        {
            for (int i = 0; i < Total; i++)
            {
                linkedList.AddLast(i);
            }
        }

        [Cleanup]
        public void Cleanup()
        {
             list.Clear();
             linkedList.Clear();
        }
    }
}
