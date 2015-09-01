using System;

namespace BenchmarkDotNet
{
    [AttributeUsage(AttributeTargets.Method)]
    public class SetupAttribute : Attribute
    {
        public SetupAttribute()
        {
        }
    }
}
