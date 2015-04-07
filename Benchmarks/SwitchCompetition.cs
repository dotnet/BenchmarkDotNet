using System.Runtime.CompilerServices;
using BenchmarkDotNet;

namespace Benchmarks
{
    public class SwitchCompetition : BenchmarkCompetition
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        private int Foo()
        {
            return 5;
        }

        private const int IterationCount = 200000003;

        [BenchmarkMethod]
        public int SwitchBig()
        {
            var foo = Foo();
            for (int i = 0; i < IterationCount; i++)
            {

                switch (foo)
                {
                    case -100:
                        foo = -100;
                        break;
                    case -200:
                        foo = -200;
                        break;
                    case -300:
                        foo = -300;
                        break;
                    case -400:
                        foo = -400;
                        break;
                    case 5:
                        foo = 5;
                        break;
                }
            }
            return foo;
        }
        [BenchmarkMethod]
        public int SwitchSmall()
        {
            var foo = Foo();
            for (int i = 0; i < IterationCount; i++)
            {

                switch (foo)
                {
                    case 1:
                        foo = 1;
                        break;
                    case 2:
                        foo = 2;
                        break;
                    case 3:
                        foo = 3;
                        break;
                    case 4:
                        foo = 4;
                        break;
                    case 5:
                        foo = 5;
                        break;
                }
            }
            return foo;
        }
    }
}