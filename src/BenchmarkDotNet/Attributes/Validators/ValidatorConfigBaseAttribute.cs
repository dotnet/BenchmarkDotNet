using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly)]
    public abstract class ValidatorConfigBaseAttribute : Attribute, IConfigSource
    {
        // CLS-Compliant Code requires a constructor without an array in the argument list
        protected ValidatorConfigBaseAttribute()
        {
            Config = ManualConfig.CreateEmpty();
        }

        protected ValidatorConfigBaseAttribute(params IValidator[] validators)
        {
            Config = ManualConfig.CreateEmpty().With(validators);
        }

        public IConfig Config { get; }
    }
}