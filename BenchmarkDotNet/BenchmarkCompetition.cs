using System;
using System.Collections.Generic;
using System.Linq;

namespace BenchmarkDotNet
{
    public class BenchmarkCompetition
    {
        private readonly List<BenchmarkCompetitionTask> tasks = new List<BenchmarkCompetitionTask>();

        public void AddTask(string name, Action initialize, Action action)
        {
            tasks.Add(new BenchmarkCompetitionTask
                {
                    Name = name,
                    Initialize = initialize,
                    Action = action
                });
        }

        public void AddTask(string name, Action action)
        {
            tasks.Add(new BenchmarkCompetitionTask
            {
                Name = name,
                Action = action
            });
        }

        public void Run()
        {
            Console.WriteLine("BenchmarkCompetition: start");
            Console.WriteLine();
            foreach (var task in tasks)
                task.Run();            
            Console.WriteLine("BenchmarkCompetition: finish");
            Console.WriteLine();
            Console.WriteLine("Competition results:");
            var nameWidth = tasks.Max(task => task.Name.Length) + 1;
            var msWidth = tasks.Max(task => task.Info.Result.AverageMilliseconds.ToString().Length);
            foreach (var task in tasks)
                Console.WriteLine("{0}: {1}ms [Error: {2:00.00}%]", 
                    task.Name.PadRight(nameWidth), 
                    task.Info.Result.AverageMilliseconds.ToString().PadLeft(msWidth),
                    task.Info.Result.DiffPercent);
        }
    }
}