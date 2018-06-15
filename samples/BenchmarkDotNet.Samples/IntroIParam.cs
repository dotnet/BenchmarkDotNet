using System.Collections.Generic;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Code;

namespace BenchmarkDotNet.Samples
{
    public class IntroIParam
    {
        public struct VeryCustomStruct
        {
            public readonly int X, Y;

            public VeryCustomStruct(int x, int y)
            {
                X = x;
                Y = y;
            }
        }

        public class CustomParam : IParam
        {
            private readonly VeryCustomStruct value;

            public CustomParam(VeryCustomStruct value) => this.value = value;

            public object Value => value;

            public string DisplayText => $"({value.X},{value.Y})";

            public string ToSourceCode() => $"new VeryCustomStruct({value.X}, {value.Y})";
        }

        [ParamsSource(nameof(Parameters))]
        public VeryCustomStruct Field;

        public IEnumerable<IParam> Parameters()
        {
            yield return new CustomParam(new VeryCustomStruct(100, 10));
            yield return new CustomParam(new VeryCustomStruct(100, 20));
            yield return new CustomParam(new VeryCustomStruct(200, 10));
            yield return new CustomParam(new VeryCustomStruct(200, 20));
        }

        [Benchmark]
        public void Benchmark() => Thread.Sleep(Field.X + Field.Y);
    }
}