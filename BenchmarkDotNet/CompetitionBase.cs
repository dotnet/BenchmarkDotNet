using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;

namespace BenchmarkDotNet
{
    [TestFixture]
    public abstract class CompetitionBase
    {
        private class TextWriterWithForcedSpaces : TextWriter
        {
            private readonly TextWriter textWriter;

            public TextWriterWithForcedSpaces(TextWriter textWriter)
            {
                this.textWriter = textWriter;
            }

            public override Encoding Encoding
            {
                get { return textWriter.Encoding; }
            }

            public override void Write(char value)
            {
                switch (value)
                {
                    case '\r':
                        return;
                    case '\n':
                        textWriter.Write("\u200b\n");
                        return;
                    case ' ':
                        textWriter.Write(" \u200b");
                        return;
                    default:
                        textWriter.Write(value);
                        return;
                }
            }
        }

        private static void AssertBenchmarkMethodHasCorrectSignature(MethodInfo method)
        {
            if (method.GetParameters().Any() || method.ReturnType != typeof (Action))
                throw new InvalidOperationException(
                    string.Format("Benchmark method {0} has incorrect signature.\n"
                                  + "Correct signature is: public Action BenchmarkName() {{ ... }}",
                        method.Name));
        }

        private static bool IsBenchmarkMethod(MethodInfo method)
        {
            return method.GetCustomAttributes(typeof (BenchmarkMethodAttribute), false).Any();
        }

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            Console.SetOut(new TextWriterWithForcedSpaces(Console.Error)); // fix for bug in NUnit+ReSharper output
        }

        [Test]
        public void CompetitionEntryPoint()
        {
            Type type = GetType();
            string benchmarkName = type.Name.WithoutSuffix("competition", StringComparison.InvariantCultureIgnoreCase);

            var competition = new BenchmarkCompetition();
            foreach (var method in type.GetMethods())
                if (IsBenchmarkMethod(method)) {
                    AssertBenchmarkMethodHasCorrectSignature(method);
                    var competitionTask = (Action) method.Invoke(this, new object[0]);
                    competition.AddTask(benchmarkName + " - " + method.Name, competitionTask);
                }

            competition.Run();
        }
    }
}