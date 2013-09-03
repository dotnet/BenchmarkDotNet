using System;
using BenchmarkDotNet;

namespace Benchmarks
{
    public class ForeachArrayProgram
    {
        public void Run()
        {
            var competition = new BenchmarkCompetition();
            int[] array = new int[500000000];
            competition.AddTask("ArrayForWithoutOptimization", () => ArrayForWithoutOptimization(array));
            competition.AddTask("ArrayForWithOptimization", () => ArrayForWithOptimization(array));
            competition.AddTask("ArrayForeach", () => ArrayForeach(array));
            competition.AddTask("ArrayForEach", () => ArrayForEach(array));
            competition.Run();
        }

        static int ArrayForWithoutOptimization(int[] array)
        {
            int sum = 0;
            for (int i = 0; i < array.Length; i++)
                sum += array[i];
            return sum;
        }

        static double ArrayForWithOptimization(int[] array)
        {
            int length = array.Length;
            int sum = 0;
            for (int i = 0; i < length; i++)
                sum += array[i];
            return sum;
        }

        static double ArrayForeach(int[] array)
        {
            int sum = 0;
            foreach (var item in array)
                sum += item;
            return sum;
        }

        static double ArrayForEach(int[] array)
        {
            int sum = 0;
            Array.ForEach(array, i => { sum += i; });
            return sum;
        }
    }
}