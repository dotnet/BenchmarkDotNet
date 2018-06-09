using System;
using System.Text;
using BenchmarkDotNet.Configs;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
    public class EncodingAttribute: Attribute, IConfigSource
    {
        public IConfig Config { get; }

        public EncodingAttribute(Encoding encoding) => Config = ManualConfig.CreateEmpty().With(encoding);

        public class Unicode: EncodingAttribute
        {
            public Unicode() : base(Encoding.Unicode) { }
        }
        
        public class ASCII: EncodingAttribute
        {
            public ASCII() : base(Encoding.ASCII) { }
        }
    }
}