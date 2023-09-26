using Microsoft.TestPlatform.AdapterUtilities;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace BenchmarkDotNet.TestAdapter
{
    /// <summary>
    /// A class that contains all the properties that can be set on VSTest TestCase and TestResults.
    /// Some of these properties are well known as they are also used by VSTest adapters for other test libraries.
    /// </summary>
    internal static class VSTestProperties
    {
        internal static readonly TestProperty Measurement = TestProperty.Register(
            "BenchmarkDotNet.TestAdapter.Measurements",
            "Measurements",
            typeof(string[]),
            TestPropertyAttributes.Hidden,
            typeof(TestResult));

        internal static readonly TestProperty TestCategoryProperty = TestProperty.Register(
            "BenchmarkDotNet.TestAdapter.TestCategory",
            "TestCategory",
            typeof(string[]),
            TestPropertyAttributes.Hidden,
            typeof(TestCase));

        internal static readonly TestProperty TestClassNameProperty = TestProperty.Register(
            "BenchmarkDotNet.TestAdapter.TestClassName",
            "ClassName",
            typeof(string),
            TestPropertyAttributes.Hidden,
            typeof(TestCase));

        internal static readonly TestProperty ManagedTypeProperty = TestProperty.Register(
            id: ManagedNameConstants.ManagedTypePropertyId,
            label: ManagedNameConstants.ManagedTypeLabel,
            category: string.Empty,
            description: string.Empty,
            valueType: typeof(string),
            validateValueCallback: o => !string.IsNullOrWhiteSpace(o as string),
            attributes: TestPropertyAttributes.Hidden,
            owner: typeof(TestCase));

        internal static readonly TestProperty ManagedMethodProperty = TestProperty.Register(
            id: ManagedNameConstants.ManagedMethodPropertyId,
            label: ManagedNameConstants.ManagedMethodLabel,
            category: string.Empty,
            description: string.Empty,
            valueType: typeof(string),
            validateValueCallback: o => !string.IsNullOrWhiteSpace(o as string),
            attributes: TestPropertyAttributes.Hidden,
            owner: typeof(TestCase));

        internal static readonly TestProperty HierarchyProperty = TestProperty.Register(
            id: HierarchyConstants.HierarchyPropertyId,
            label: HierarchyConstants.HierarchyLabel,
            category: string.Empty,
            description: string.Empty,
            valueType: typeof(string[]),
            validateValueCallback: null,
            attributes: TestPropertyAttributes.Immutable,
            owner: typeof(TestCase));
    }
}
