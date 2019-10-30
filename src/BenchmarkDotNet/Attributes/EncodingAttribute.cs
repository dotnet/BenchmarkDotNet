using System;
using System.Text;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Loggers;
using JetBrains.Annotations;

#pragma warning disable CS3015 // no public ctor with CLS-compliant arguments
namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
    [Obsolete]
    public class EncodingAttribute : Attribute, IConfigSource
    {
        public IConfig Config { get; }

        private EncodingAttribute(Encoding encoding)
        {
            Config = Equals(encoding, Encoding.Unicode)
                ? ManualConfig.CreateEmpty().With(ConsoleLogger.Unicode)
                : ManualConfig.CreateEmpty();
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