using AwesomeAssertions;
using BenchmarkDotNet.ConsoleArguments;
using BenchmarkDotNet.Extensions;
using CommandLine;
using System.Reflection;

namespace BenchmarkDotNet.Tests;

public class CommandLineOptionsTests
{
    private sealed record OptionProperty(PropertyInfo Property, OptionAttribute Option);

    /// <summary>
    /// Validates that the hardcoded set in CommandLineOptions (MultiValueOptionNames)
    /// is consistent with the actual option definitions in the CommandLineOptions class.
    /// </summary>
    [Fact]
    public void ValidateCommandLineOptionDefinitions()
    {
        // Arrange
        // Collect all option definitions of CommandLineOptions via reflection.
        var optionProperties = typeof(CommandLineOptions)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(property => (Property: property, Option: property.GetCustomAttribute<OptionAttribute>()))
            .Where(entry => entry.Option is not null)
            .Select(x => new OptionProperty(x.Property, x.Option!))
            .ToArray();

        // Act
        var multiValueOptionNames = CommandLineOptions.MultiValueOptions.ToArray();

        // Assert
        // Validate MultiValueOptionNames definitions are consistent with the property types.
        foreach (var (property, option) in optionProperties)
        {
            bool isMultiValue = property.PropertyType != typeof(string) && typeof(System.Collections.IEnumerable).IsAssignableFrom(property.PropertyType);
            if (isMultiValue)
                multiValueOptionNames.Should().Contain(option.LongName);
            else
                multiValueOptionNames.Should().NotContain(option.LongName);
        }
    }
}
