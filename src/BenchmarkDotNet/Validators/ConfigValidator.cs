using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Loggers;

namespace BenchmarkDotNet.Validators
{
    public class ConfigValidator : IValidator
    {
        public static readonly IValidator DontFailOnError = new ConfigValidator();

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

            var pathValidation = ValidateArtifactsPath(validationParameters.Config.ArtifactsPath);

            if (pathValidation != null)
                yield return pathValidation;
        }

        private static ValidationError ValidateArtifactsPath(string artifactsPath)
        {
            if (artifactsPath == null) // null is OK, default path will be used
                return null;

            if (string.IsNullOrWhiteSpace(artifactsPath))
                return new ValidationError(true, $"The ArtifactsPath is empty or whitespace. Use null to get the default value ({DefaultConfig.Instance.ArtifactsPath}).");
            if (artifactsPath.IndexOfAny(Path.GetInvalidPathChars()) != -1)
                return new ValidationError(true, $"The ArtifactsPath contains invalid path characters (one of {string.Join(",", Path.GetInvalidPathChars().Select(@char => $"'{@char}'"))}).");

            try
            {
                // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                Path.GetFullPath(artifactsPath);
            }
            catch (PathTooLongException)
            {
                return new ValidationError(true, "The ArtifactsPath is too long.");
            }
            catch (SecurityException)
            {
                return new ValidationError(true, $"You don't have the required permission to access ArtifactsPath ({artifactsPath}).");
            }

            return null;
        }
    }
}