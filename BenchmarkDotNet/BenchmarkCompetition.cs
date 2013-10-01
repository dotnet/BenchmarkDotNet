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
            PrintResults();
        }

        public void PrintResults()
        {
            var nameWidth = tasks.Max(task => task.Name.Length) + 1;
            var msWidth = tasks.Max(task => task.Info.Result.MedianMilliseconds.ToString().Length);
            var ticksWidth = tasks.Max(task => task.Info.Result.MedianTicks.ToString().Length);
            var stdDevWidth = tasks.Max(task => string.Format("{0:0.00}", task.Info.Result.StandardDeviationMilliseconds).Length);
            foreach (var task in tasks)
            {
                if (BenchmarkSettings.Instance.DetailedMode)
                    ConsoleHelper.WriteLineStatistic("{0}: {1}ms {2} ticks [Error = {3:00.00}%, StdDev = {4}]",
                        task.Name.PadRight(nameWidth),
                        task.Info.Result.MedianMilliseconds.ToString().PadLeft(msWidth),
                        task.Info.Result.MedianTicks.ToString().PadLeft(ticksWidth),
                        task.Info.Result.Error * 100,
                        string.Format("{0:0.00}", task.Info.Result.StandardDeviationMilliseconds).PadLeft(stdDevWidth));
                else
                    ConsoleHelper.WriteLineStatistic("{0}: {1}ms",
                        task.Name.PadRight(nameWidth),
                        task.Info.Result.MedianMilliseconds.ToString().PadLeft(msWidth));
            }
        }
    }
}