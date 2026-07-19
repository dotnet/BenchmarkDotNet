using System.Runtime.InteropServices;
using Xunit.Sdk;
using Xunit.v3;

[assembly: Guid("74362bb1-9f64-4be5-b079-b4ac19dae5db")]

[assembly: CaptureConsole]
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly)]
[assembly: Parallelization(Mode = ParallelMode.None)]
