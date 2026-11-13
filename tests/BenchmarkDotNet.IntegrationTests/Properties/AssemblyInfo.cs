using BenchmarkDotNet.IntegrationTests.Xunit;
using System.Runtime.InteropServices;
using Xunit.Sdk;
using Xunit.v3;

[assembly: Guid("74362bb1-9f64-4be5-b079-b4ac19dae5db")]

[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly)]
[assembly: Parallelization(Mode = ParallelMode.None)]
[assembly: TestCollectionOrderer(typeof(CustomTestCollectionOrderer))]
