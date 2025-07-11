using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace BenchmarkDotNet.TestAdapter;

internal class TestCaseFilter
{
    private const string DisplayNameString = "DisplayName";
    private const string FullyQualifiedNameString = "FullyQualifiedName";

    private readonly HashSet<string> knownTraits;
    private List<string> supportedPropertyNames;
    private readonly ITestCaseFilterExpression? filterExpression;
    private readonly bool successfullyGotFilter;
    private readonly bool isDiscovery;

    public TestCaseFilter(IDiscoveryContext discoveryContext, LoggerHelper logger)
    {
        // Traits are not known at discovery time because we load them from benchmarks
        isDiscovery = true;
        knownTraits = [];
        supportedPropertyNames = GetSupportedPropertyNames();
        successfullyGotFilter = GetTestCaseFilterExpressionFromDiscoveryContext(discoveryContext, logger, out filterExpression);
    }

    public TestCaseFilter(IRunContext runContext, LoggerHelper logger, string assemblyFileName, HashSet<string> knownTraits)
    {
        this.knownTraits = knownTraits;
        supportedPropertyNames = GetSupportedPropertyNames();
        successfullyGotFilter = GetTestCaseFilterExpression(runContext, logger, assemblyFileName, out filterExpression);
    }

    public string GetTestCaseFilterValue()
    {
        return successfullyGotFilter
            ? filterExpression?.TestCaseFilterValue ?? ""
            : "";
    }

    public bool MatchTestCase(TestCase testCase)
    {
        if (!successfullyGotFilter)
        {
            // Had an error while getting filter, match no testcase to ensure discovered test list is empty
            return false;
        }
        else if (filterExpression == null)
        {
            // No filter specified, keep every testcase
            return true;
        }

        return filterExpression.MatchTestCase(testCase, p => PropertyProvider(testCase, p));
    }

    public object? PropertyProvider(TestCase testCase, string name)
    {
        // Traits filtering
        if (isDiscovery || knownTraits.Contains(name))
        {
            var result = new List<string>();

            foreach (var trait in GetTraits(testCase))
                if (string.Equals(trait.Key, name, StringComparison.OrdinalIgnoreCase))
                    result.Add(trait.Value);

            if (result.Count > 0)
                return result.ToArray();
        }

        // Property filtering
        switch (name.ToLowerInvariant())
        {
            // FullyQualifiedName
            case "fullyqualifiedname":
                return testCase.FullyQualifiedName;
            // DisplayName
            case "displayname":
                return testCase.DisplayName;
            default:
                return null;
        }
    }

    private bool GetTestCaseFilterExpression(IRunContext runContext, LoggerHelper logger, string assemblyFileName, out ITestCaseFilterExpression? filter)
    {
        filter = null;

        try
        {
            filter = runContext.GetTestCaseFilter(supportedPropertyNames, null!);
            return true;
        }
        catch (TestPlatformFormatException e)
        {
            logger.LogWarning("{0}: Exception filtering tests: {1}", Path.GetFileNameWithoutExtension(assemblyFileName), e.Message);
            return false;
        }
    }

    private bool GetTestCaseFilterExpressionFromDiscoveryContext(IDiscoveryContext discoveryContext, LoggerHelper logger, out ITestCaseFilterExpression? filter)
    {
        filter = null;

        if (discoveryContext is IRunContext runContext)
        {
            try
            {
                filter = runContext.GetTestCaseFilter(supportedPropertyNames, null!);
                return true;
            }
            catch (TestPlatformException e)
            {
                logger.LogWarning("Exception filtering tests: {0}", e.Message);
                return false;
            }
        }
        else
        {
            try
            {
                // GetTestCaseFilter is present on DiscoveryContext but not in IDiscoveryContext interface
                var method = discoveryContext.GetType().GetRuntimeMethod("GetTestCaseFilter", [typeof(IEnumerable<string>), typeof(Func<string, TestProperty>)]);
                filter = (ITestCaseFilterExpression)method?.Invoke(discoveryContext, [supportedPropertyNames, null])!;

                return true;
            }
            catch (TargetInvocationException e)
            {
                if (e?.InnerException is TestPlatformException ex)
                {
                    logger.LogWarning("Exception filtering tests: {0}", ex.InnerException.Message ?? "");
                    return false;
                }

                throw e!.InnerException;
            }
        }
    }

    private List<string> GetSupportedPropertyNames()
    {
        // Returns the set of well-known property names usually used with the Test Plugins (Used Test Traits + DisplayName + FullyQualifiedName)
        if (supportedPropertyNames == null)
        {
            supportedPropertyNames = knownTraits.ToList();
            supportedPropertyNames.Add(DisplayNameString);
            supportedPropertyNames.Add(FullyQualifiedNameString);
        }

        return supportedPropertyNames;
    }

    private static IEnumerable<KeyValuePair<string, string>> GetTraits(TestCase testCase)
    {
        var traitProperty = TestProperty.Find("TestObject.Traits");
        return traitProperty != null
            ? testCase.GetPropertyValue(traitProperty, Array.Empty<KeyValuePair<string, string>>())
            : [];
    }
}
