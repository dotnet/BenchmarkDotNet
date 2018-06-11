using System;
using System.Text;
using BenchmarkDotNet.Configs;
using JetBrains.Annotations;

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