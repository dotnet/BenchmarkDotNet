using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess;
using System;
using BenchmarkDotNet.Loggers;

namespace BenchmarkDotNet.Validators
{
    public class ConfigValidator : IValidator
    {
        public static readonly IValidator Default = new ConfigValidator();

        private ConfigValidator() { }

        public bool TreatsWarningsAsErrors => false;

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters)
        {
            if (validationParameters.Config.GetLoggers().IsEmpty())
            {
                const string errorMessage = "No loggers defined, you will not see any progress!";

                ConsoleLogger.Default.WriteLineError(errorMessage); // no loggers defined, so we need to somehow display this information

                yield return new ValidationError(false, errorMessage);
            }

            if (validationParameters.Config.GetExporters().IsEmpty())
                yield return new ValidationError(false, "No exporters defined, results will not be persisted.");
            if (validationParameters.Config.GetColumnProviders().IsEmpty())
                yield return new ValidationError(false, "No column providers defined, result table will be empty.");
        }
    }
}