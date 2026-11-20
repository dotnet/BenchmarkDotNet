using BenchmarkDotNet.IntegrationTests.Xunit;

[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly, DisableTestParallelization = true)]
[assembly: TestCollectionOrderer(CustomTestCollectionOrderer.TypeName, CustomTestCollectionOrderer.AssembyName)]
