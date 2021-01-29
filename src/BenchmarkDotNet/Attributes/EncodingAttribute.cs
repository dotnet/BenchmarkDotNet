using System;
using System.Text;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Loggers;
using JetBrains.Annotations;

#pragma warning disable CS3015 // no public ctor with CLS-compliant arguments
namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
    [Obsolete("Don't use it")]
    public class EncodingAttribute : Attribute, IConfigSource
    {
        public IConfig Config { get; }

        private EncodingAttribute(Encoding encoding)
        {
            if (Equals(encoding, Encoding.Unicode))
            {
                Config = ManualConfig.CreateEmpty().AddLogger(ConsoleLogger.Unicode);
                return;
            }

            if (Equals(encoding, Encoding.ASCII))
            {
                Config = ManualConfig.CreateEmpty();
                return;
            }

            throw new ArgumentOutOfRangeException(nameof(encoding), encoding.ToString(), "Only ASCII and Unicode encoding are supported");
        }

        [PublicAPI]
        public class Unicode : EncodingAttribute
        {
            public Unicode() : base(Encoding.Unicode) { }
        }

        [PublicAPI]
        public class ASCII : EncodingAttribute
        {
            public ASCII() : base(Encoding.ASCII) { }
        }
    }
}
#pragma warning restore CS3015
