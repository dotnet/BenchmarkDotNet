using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Validators;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly)]
    public abstract class ValidatorConfigBaseAttribute : Attribute, IConfigSource
    {
        // CLS-Compliant Code requires a constructor without an array in the argument list
        [PublicAPI]
        protected ValidatorConfigBaseAttribute()
        {
            Config = ManualConfig.CreateEmpty();
        }

        protected ValidatorConfigBaseAttribute(params IValidator[] validators)
        {
            Config = ManualConfig.CreateEmpty().AddValidator(validators);
        }

        public IConfig Config { get; }
    }
}