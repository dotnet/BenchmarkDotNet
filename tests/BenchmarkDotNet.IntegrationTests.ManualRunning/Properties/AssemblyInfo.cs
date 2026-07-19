using System.Runtime.InteropServices;
using Xunit.Sdk;
using Xunit.v3;

[assembly: Guid("4d7c0994-2750-45dc-bbe8-750b158ea826")]

[assembly: CaptureConsole]
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly)]
[assembly: Parallelization(Mode = ParallelMode.None)]
