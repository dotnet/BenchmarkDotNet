using System;
using System.IO;
using System.Text;
using NUnit.Framework;

namespace BenchmarkDotNet
{
    [TestFixture]
    public abstract class BenchmarkCompetitionTests : BenchmarkCompetition
    {
        #region NUnit

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


        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            Console.SetOut(new TextWriterWithForcedSpaces(Console.Error)); // fix for error in NUnit+ReSharper output
        }

        [Test]
        public override void Run()
        {
            base.Run();
        }

        #endregion

    }
}