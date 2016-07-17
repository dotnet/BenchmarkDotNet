using System;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class SetupAttribute : Attribute
    {
        public SetupAttribute()
        {
        }
    }
}
