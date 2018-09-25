using System;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Mathematics;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Attributes
{
    [PublicAPI]
    public class WelchTTestRelativeAttribute : ColumnConfigBaseAttribute
    {
        public WelchTTestRelativeAttribute(double ratio = 0.01)
            : base(BaselineScaledColumn.CreateWelchTTest(new RelativeHypothesis(ratio))) { }
    }

    [PublicAPI]
    public class WelchTTestAbsoluteNanosecondsAttribute : ColumnConfigBaseAttribute
    {
        public WelchTTestAbsoluteNanosecondsAttribute(double threshold)
            : base(BaselineScaledColumn.CreateWelchTTest(new AbsoluteHypothesis(TimeInterval.FromNanoseconds(threshold)))) { }
    }

    [PublicAPI]
    public class WelchTTestAbsoluteMicrosecondsAttribute : ColumnConfigBaseAttribute
    {
        public WelchTTestAbsoluteMicrosecondsAttribute(double threshold)
            : base(BaselineScaledColumn.CreateWelchTTest(new AbsoluteHypothesis(TimeInterval.FromMicroseconds(threshold)))) { }
    }

    [PublicAPI]
    public class WelchTTestAbsoluteMillisecondsAttribute : ColumnConfigBaseAttribute
    {
        public WelchTTestAbsoluteMillisecondsAttribute(double threshold)
            : base(BaselineScaledColumn.CreateWelchTTest(new AbsoluteHypothesis(TimeInterval.FromMilliseconds(threshold)))) { }
    }

    [PublicAPI]
    public class WelchTTestAbsoluteSecondsAttribute : ColumnConfigBaseAttribute
    {
        public WelchTTestAbsoluteSecondsAttribute(double threshold)
            : base(BaselineScaledColumn.CreateWelchTTest(new AbsoluteHypothesis(TimeInterval.FromSeconds(threshold)))) { }
    }

    [Obsolete("Use WelchTTestRelativeAttribute")]
    public class WelchTTestPValueColumnAttribute : WelchTTestRelativeAttribute { }
}