using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Jobs;

namespace BenchmarkDotNet.Samples.IL
{
    // See: http://codeblog.jonskeet.uk/2014/07/16/micro-optimization-the-surprising-inefficiency-of-readonly-fields/
    [AllJitsJob]
    public class IL_ReadonlyFields
    {
        public struct Int256
        {
            private readonly long bits0, bits1, bits2, bits3;

            public Int256(long bits0, long bits1, long bits2, long bits3)
            {
                this.bits0 = bits0;
                this.bits1 = bits1;
                this.bits2 = bits2;
                this.bits3 = bits3;
            }

            public long Bits0 { get { return bits0; } }
            public long Bits1 { get { return bits1; } }
            public long Bits2 { get { return bits2; } }
            public long Bits3 { get { return bits3; } }
        }

        private readonly Int256 readOnlyField = new Int256(1L, 5L, 10L, 100L);
        private Int256 field = new Int256(1L, 5L, 10L, 100L);

        [Benchmark]
        public long GetValue()
        {
            return field.Bits0 + field.Bits1 + field.Bits2 + field.Bits3;
        }

        [Benchmark]
        public long GetReadOnlyValue()
        {
            return readOnlyField.Bits0 + readOnlyField.Bits1 + readOnlyField.Bits2 + readOnlyField.Bits3;
        }
    }
}