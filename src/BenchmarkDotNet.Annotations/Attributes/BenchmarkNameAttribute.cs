using System;
using System.Collections.Generic;
using System.Text;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class BenchmarkNameAttribute : Attribute
    {
        public BenchmarkNameAttribute(){        }
        public BenchmarkNameAttribute(string name)
            => Name = name;

        public string Name { get; set; }
    }
}
