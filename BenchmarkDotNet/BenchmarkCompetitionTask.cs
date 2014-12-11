using System;

namespace BenchmarkDotNet
{
    public class BenchmarkCompetitionTask
    {
        public string Name { get; set; }
        public Action Initialize { get; set; }
        public Action Action { get; set; }
        public Action Clean { get; set; }

        public BenchmarkInfo Info { get; private set; }

        public void Run()
        {
            ConsoleHelper.WriteLineHeader("***** {0}: start *****", Name);
            if (Initialize != null)
                Initialize();
            Info = new Benchmark().Run(Action);
            if (Clean != null)
                Clean();
            ConsoleHelper.WriteLineHeader("***** {0}: end *****", Name);
            ConsoleHelper.NewLine();
        }
    }
}