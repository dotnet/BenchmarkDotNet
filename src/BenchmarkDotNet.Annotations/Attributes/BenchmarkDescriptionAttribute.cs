using System;
using System.Collections.Generic;
using System.Text;

namespace BenchmarkDotNet.Attributes
{
    public class BenchmarkDescriptionAttribute : Attribute
    {
        public BenchmarkDescriptionAttribute(){        }
        public BenchmarkDescriptionAttribute(string description)
            => Description = description;

        public string Description { get; set; }
    }
}
