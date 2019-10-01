using System;
using System.Text;
using BenchmarkDotNet.Configs;
using JetBrains.Annotations;

#pragma warning disable CS3015 // no public ctor with CLS-compliant arguments
namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
    public class EncodingAttribute: Attribute, IConfigSource
    {
        public IConfig Config { get; }

        private EncodingAttribute(Encoding encoding) => Config = ManualConfig.CreateEmpty().With(encoding);

        [PublicAPI]
        public class Unicode: EncodingAttribute
        {
            public Unicode() : base(Encoding.Unicode) { }
        }

        [PublicAPI]
        public class ASCII: EncodingAttribute
        {
            public ASCII() : base(Encoding.ASCII) { }
        }
    }
}
#pragma warning restore CS3015