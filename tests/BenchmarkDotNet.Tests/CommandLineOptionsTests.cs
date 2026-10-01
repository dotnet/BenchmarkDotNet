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
    /// Validates that the hardcoded dictionaries in CommandLineOptions (CanonicalNames, MultiInstanceOptionNames)
    /// are consistent with the actual option definitions in the CommandLineOptions class.
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

        // Gets all long names.
        var longNames = optionProperties
            .Select(entry => entry.Option.LongName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Gets all names (long and short)
        var allNames = longNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var option in optionProperties.Where(x => x.Option.ShortName.IsNotBlank()))
            allNames.Add(option.Option.ShortName);

        // Act
        var canonicalNames = CommandLineOptions.CanonicalNames;
        var multiInstanceOptionNames = CommandLineOptions.MultiValueOptionNames;

        // Assert
        // Validate CanonicalNames definitions.
        canonicalNames.Keys.Should().BeEquivalentTo(allNames);
        canonicalNames.Values.Distinct().Should().BeEquivalentTo(longNames);

        // Validate each short/long name resolves to its own canonical long name
        // (Without this test, a swapped alias would pass the set checks above).
        var expectedCanonicals = optionProperties
            .SelectMany(entry => new[]
            {
                (Name: entry.Option!.LongName, Canonical: entry.Option.LongName),
                (Name: entry.Option.ShortName, Canonical: entry.Option.LongName)
            })
            .Where(entry => entry.Name.IsNotBlank())
            .ToArray();

        foreach (var (name, expected) in expectedCanonicals)
        {
            bool isSuccess = canonicalNames.TryGetValue(name, out string? actual);
            isSuccess.Should().BeTrue($"Name '{name}' should be resolve from CanonicalNames.");
            actual.Should().Be(expected, $"Name '{name}' should resolve to '{expected}'.");
        }

        // Validate MultiInstanceOptionNames definitions are consistent with the property types.
        foreach (var (property, option) in optionProperties)
        {
            bool isMultiValue = property.PropertyType != typeof(string) && typeof(System.Collections.IEnumerable).IsAssignableFrom(property.PropertyType);
            if (isMultiValue)
                multiInstanceOptionNames.Should().Contain(option.LongName);
            else
                multiInstanceOptionNames.Should().NotContain(option.LongName);
        }
    }
}
