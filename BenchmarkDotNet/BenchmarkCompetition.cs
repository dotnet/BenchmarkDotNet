using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BenchmarkDotNet
{
    public class BenchmarkCompetition
    {
        #region Tasks

        private readonly List<BenchmarkCompetitionTask> tasks = new List<BenchmarkCompetitionTask>();
        private bool methodTasksWasAdded;

        public void AddTask(string name, Action initialize, Action action, Action clean)
        {
            tasks.Add(new BenchmarkCompetitionTask
                {
                    Name = name,
                    Initialize = initialize,
                    Action = action,
                    Clean = clean
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

        public void AddTask(BenchmarkCompetitionTask task)
        {
            tasks.Add(task);
        }

        #endregion

        #region Run and results

        public virtual string Name
        {
            get { return GetType().Name.WithoutSuffix("Competition"); }
        }

        protected virtual void Prepare()
        {
        }

        public virtual void Run()
        {
            if (!methodTasksWasAdded)
            {
                tasks.AddRange(GetMethodTasks());
                methodTasksWasAdded = true;
            }
            Prepare();
            ConsoleHelper.WriteLineHeader("BenchmarkCompetition {0}: start", Name);
            ConsoleHelper.NewLine();
            foreach (var task in tasks)
                task.Run();
            ConsoleHelper.WriteLineHeader("BenchmarkCompetition {0}: finish", Name);
            ConsoleHelper.NewLine();
            ConsoleHelper.WriteLineHeader("Competition results:");
            PrintResults();
        }

        public void PrintResults()
        {
            var nameWidth = tasks.Max(task => task.Name.Length) + 1;
            var msWidth = tasks.Max(task => task.Info.Result.MedianMilliseconds.ToCultureString().Length);
            var ticksWidth = tasks.Max(task => task.Info.Result.MedianTicks.ToCultureString().Length);
            var stdDevWidth = tasks.Max(task => BenchmarkUtils.CultureFormat("{0:0.00}", task.Info.Result.StandardDeviationMilliseconds).Length);
            foreach (var task in tasks)
            {
                if (BenchmarkSettings.Instance.DetailedMode)
                    ConsoleHelper.WriteLineStatistic("{0}: {1}ms, {2} ticks [Error = {3:00.00}%, StdDev = {4}]",
                        task.Name.PadRight(nameWidth),
                        task.Info.Result.MedianMilliseconds.ToCultureString().PadLeft(msWidth),
                        task.Info.Result.MedianTicks.ToCultureString().PadLeft(ticksWidth),
                        task.Info.Result.Error * 100,
                        BenchmarkUtils.CultureFormat("{0:0.00}", task.Info.Result.StandardDeviationMilliseconds).PadLeft(stdDevWidth));
                else
                    ConsoleHelper.WriteLineStatistic("{0}: {1}ms",
                        task.Name.PadRight(nameWidth),
                        task.Info.Result.MedianMilliseconds.ToCultureString().PadLeft(msWidth));
            }
        }

        #endregion

        #region BenchmarkMethods

        private static void AssertBenchmarkMethodHasCorrectSignature(MethodInfo methodInfo)
        {
            if (methodInfo.GetParameters().Any())
                throw new InvalidOperationException(
                    string.Format("Benchmark method {0} has incorrect signature.\n"
                                  + "Method shouldn't have any arguments.",
                        methodInfo.Name));
        }

        private static BenchmarkMethodAttribute GetBenchmarkMethodAttribute(MethodInfo methodInfo)
        {
            return methodInfo.GetCustomAttributes(typeof(BenchmarkMethodAttribute), false).FirstOrDefault() as BenchmarkMethodAttribute;
        }

        private MethodInfo GetBenchmarkMethodInitialize(string name)
        {
            return (
                from methodInfo in GetType().GetMethods()
                let attribute = methodInfo.GetCustomAttributes(typeof(BenchmarkMethodInitializeAttribute), false).
                                OfType<BenchmarkMethodInitializeAttribute>().FirstOrDefault()
                where attribute != null && (attribute.Name == name || methodInfo.Name == name + "Initialize")
                select methodInfo).FirstOrDefault();
        }

        private MethodInfo GetBenchmarkMethodClean(string name)
        {
            return (
                from methodInfo in GetType().GetMethods()
                let attribute = methodInfo.GetCustomAttributes(typeof(BenchmarkMethodCleanAttribute), false).
                                OfType<BenchmarkMethodCleanAttribute>().FirstOrDefault()
                where attribute != null && (attribute.Name == name || methodInfo.Name == name + "Clean")
                select methodInfo).FirstOrDefault();
        }

        private IEnumerable<BenchmarkCompetitionTask> GetMethodTasks()
        {
            var methods = GetType().GetMethods();
            for (int i = 0; i < methods.Length; i++)
            {
                var methodInfo = methods[i];
                var benchmarkMethodAttribute = GetBenchmarkMethodAttribute(methodInfo);
                if (benchmarkMethodAttribute != null)
                {
                    AssertBenchmarkMethodHasCorrectSignature(methodInfo);
                    var name = benchmarkMethodAttribute.Name ?? methodInfo.Name;
                    Action action = () => methodInfo.Invoke(this, new object[0]);

                    Action initialize = null;
                    var methodInfoInitialize = GetBenchmarkMethodInitialize(name);
                    if (methodInfoInitialize != null)
                    {
                        AssertBenchmarkMethodHasCorrectSignature(methodInfoInitialize);
                        initialize = () => methodInfoInitialize.Invoke(this, new object[0]);
                    }

                    Action clean = null;
                    var methodInfoClean = GetBenchmarkMethodClean(name);
                    if (methodInfoClean != null)
                    {
                        AssertBenchmarkMethodHasCorrectSignature(methodInfoClean);
                        clean = () => methodInfoClean.Invoke(this, new object[0]);
                    }

                    yield return new BenchmarkCompetitionTask { Name = name, Initialize = initialize, Action = action, Clean = clean };
                }
            }
        }

        #endregion
    }
}