using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet;

namespace Benchmarks
{
    public class ForeachListProgram
    {
        public void Run(Manager manager)
        {
            var competition = new BenchmarkCompetition();
            var list = Enumerable.Range(0, 200000000).ToList();
            competition.AddTask("ListForWithoutOptimization", () => ListForWithoutOptimization(list));
            competition.AddTask("ListForWithOptimization", () => ListForWithOptimization(list));
            competition.AddTask("ListForeach", () => ListForeach(list));
            competition.AddTask("ListForEach", () => ListForEach(list));
            competition.Run();
            manager.ProcessCompetition(competition);
        }

        static int ListForWithoutOptimization(List<int> list)
        {
            int sum = 0;
            for (int i = 0; i < list.Count; i++)
                sum += list[i];
            return sum;
        }

        static double ListForWithOptimization(List<int> list)
        {
            int length = list.Count;
            int sum = 0;
            for (int i = 0; i < length; i++)
                sum += list[i];
            return sum;
        }

        static double ListForeach(List<int> list)
        {
            int sum = 0;
            foreach (var item in list)
                sum += item;
            return sum;
        }

        static double ListForEach(List<int> list)
        {
            int sum = 0;
            list.ForEach(i => { sum += i; });
            return sum;
        }
    }
}