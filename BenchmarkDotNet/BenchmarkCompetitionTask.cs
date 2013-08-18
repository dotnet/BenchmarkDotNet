using System;

namespace BenchmarkDotNet
{
    public class BenchmarkCompetitionTask
    {
        public string Name { get; set; }
        public Action Initialize { get; set; }
        public Action Action { get; set; }

        public BenchmarkInfo Info { get; private set; }

        public void Run()
        {
            Console.WriteLine("***** {0}: start *****", Name);
            if (Initialize != null)
                Initialize();
            Info = new Benchmark().Run(Action);
            Console.WriteLine("***** {0}: end *****", Name);
            Console.WriteLine();
        }
    }
}