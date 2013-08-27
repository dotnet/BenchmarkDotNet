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
            ConsoleHelper.WriteLineHeader("BenchmarkCompetition: start");
            ConsoleHelper.NewLine();
            foreach (var task in tasks)
                task.Run();
            ConsoleHelper.WriteLineHeader("BenchmarkCompetition: finish");
            ConsoleHelper.NewLine();
            ConsoleHelper.WriteLineHeader("Competition results:");
            var nameWidth = tasks.Max(task => task.Name.Length) + 1;
            var msWidth = tasks.Max(task => task.Info.Result.MedianMilliseconds.ToString().Length);
            foreach (var task in tasks)
                ConsoleHelper.WriteLineStatistic("{0}: {1}ms [Error: {2:00.00}%]",
                    task.Name.PadRight(nameWidth),
                    task.Info.Result.MedianMilliseconds.ToString().PadLeft(msWidth),
                    task.Info.Result.Error * 100);
        }
    }
}