using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.EventHandlers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.Results;
using BenchmarkDotNet.Validators;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace BenchmarkDotNet.IntegrationTests
{
    public class BenchmarkEventHandlerTests
    {
        [Fact]
        public void WhenUsingEventHandlerAndNoBenchmarks()
        {
            var events = RunBenchmarksAndRecordEvents(new[] { typeof(ClassEmpty) });
            Assert.Single(events);
            Assert.Equal(nameof(BenchmarkEventHandlerBase.OnStartValidationStage), events[0].EventType);
        }

        [Fact]
        public void WhenUsingEventHandlerOnSingleClass()
        {
            var events = RunBenchmarksAndRecordEvents(new[] { typeof(ClassA) });

            Assert.Equal(12, events.Count);

            Assert.Equal(nameof(BenchmarkEventHandlerBase.OnStartValidationStage), events[0].EventType);
            Assert.Equal(nameof(BenchmarkEventHandlerBase.OnEndValidationStage), events[1].EventType);
            Assert.Equal(nameof(BenchmarkEventHandlerBase.OnStartBuildStage), events[2].EventType);
            Assert.Equal(nameof(BenchmarkEventHandlerBase.OnEndBuildStage), events[3].EventType);
            Assert.Equal(nameof(BenchmarkEventHandlerBase.OnStartRunStage), events[4].EventType);

            var benchmarkTypeAndMethods = new List<(Type Type, string[] MethodNames)>
            {
                (typeof(ClassA), new[]{ nameof(ClassA.Method1), nameof(ClassA.Method2) })
            };

            int eventIndex = 5;
            foreach ((var type, var methodNames) in benchmarkTypeAndMethods)
            {
                Assert.Equal(nameof(BenchmarkEventHandlerBase.OnStartRunBenchmarksInType), events[eventIndex].EventType);
                Assert.Equal(type, events[eventIndex++].Args[0] as Type);

                foreach (var method in methodNames)
                {
                    var methodDescriptor = type.GetMethod(method);
                    Assert.Equal(nameof(BenchmarkEventHandlerBase.OnStartRunBenchmark), events[eventIndex].EventType);
                    Assert.Equal(methodDescriptor, (events[eventIndex++].Args[0] as BenchmarkCase).Descriptor.WorkloadMethod);

                    Assert.Equal(nameof(BenchmarkEventHandlerBase.OnEndRunBenchmark), events[eventIndex].EventType);
                    Assert.Equal(methodDescriptor, (events[eventIndex++].Args[0] as BenchmarkCase).Descriptor.WorkloadMethod);
                }

                Assert.Equal(nameof(BenchmarkEventHandlerBase.OnEndRunBenchmarksInType), events[eventIndex].EventType);
                Assert.Equal(type, events[eventIndex++].Args[0] as Type);
            }

            Assert.Equal(nameof(BenchmarkEventHandlerBase.OnEndRunStage), events[eventIndex].EventType);
        }

        [Fact]
        public void WhenUsingEventHandlerOnMultipleClasses()
        {
            var events = RunBenchmarksAndRecordEvents(new[] { typeof(ClassA), typeof(ClassB) });

            Assert.Equal(22, events.Count);

            Assert.Equal(nameof(BenchmarkEventHandlerBase.OnStartValidationStage), events[0].EventType);
            Assert.Equal(nameof(BenchmarkEventHandlerBase.OnEndValidationStage), events[1].EventType);
            Assert.Equal(nameof(BenchmarkEventHandlerBase.OnStartBuildStage), events[2].EventType);
            Assert.Equal(nameof(BenchmarkEventHandlerBase.OnEndBuildStage), events[3].EventType);
            Assert.Equal(nameof(BenchmarkEventHandlerBase.OnStartRunStage), events[4].EventType);

            var benchmarkTypeAndMethods = new List<(Type Type, string[] MethodNames)>
            {
                (typeof(ClassA), new[]{ nameof(ClassA.Method1), nameof(ClassA.Method2) }),
                (typeof(ClassB), new[]{ nameof(ClassB.Method1), nameof(ClassB.Method2), nameof(ClassB.Method3), nameof(ClassB.Method4) })
            };

            int eventIndex = 5;
            foreach ((var type, var methodNames) in benchmarkTypeAndMethods)
            {
                Assert.Equal(nameof(BenchmarkEventHandlerBase.OnStartRunBenchmarksInType), events[eventIndex].EventType);
                Assert.Equal(type, events[eventIndex++].Args[0] as Type);

                foreach (var method in methodNames)
                {
                    var methodDescriptor = type.GetMethod(method);
                    Assert.Equal(nameof(BenchmarkEventHandlerBase.OnStartRunBenchmark), events[eventIndex].EventType);
                    Assert.Equal(methodDescriptor, (events[eventIndex++].Args[0] as BenchmarkCase).Descriptor.WorkloadMethod);

                    Assert.Equal(nameof(BenchmarkEventHandlerBase.OnEndRunBenchmark), events[eventIndex].EventType);
                    Assert.Equal(methodDescriptor, (events[eventIndex++].Args[0] as BenchmarkCase).Descriptor.WorkloadMethod);
                }

                Assert.Equal(nameof(BenchmarkEventHandlerBase.OnEndRunBenchmarksInType), events[eventIndex].EventType);
                Assert.Equal(type, events[eventIndex++].Args[0] as Type);
            }

            Assert.Equal(nameof(BenchmarkEventHandlerBase.OnEndRunStage), events[eventIndex].EventType);
        }

        [Fact]
        public void WhenUsingEventHandlerWithValidationErrors()
        {
            var validator = new ErrorAllCasesValidator();
            var events = RunBenchmarksAndRecordEvents(new[] { typeof(ClassA) }, validator);

            Assert.Equal(14, events.Count);
            Assert.Equal(nameof(BenchmarkEventHandlerBase.OnStartValidationStage), events[0].EventType);
            Assert.Equal(nameof(BenchmarkEventHandlerBase.OnValidationError), events[1].EventType);
            Assert.Equal(typeof(ClassA).GetMethod(nameof(ClassA.Method1)), (events[1].Args[0] as ValidationError).BenchmarkCase.Descriptor.WorkloadMethod);
            Assert.Equal(nameof(BenchmarkEventHandlerBase.OnValidationError), events[2].EventType);
            Assert.Equal(typeof(ClassA).GetMethod(nameof(ClassA.Method2)), (events[2].Args[0] as ValidationError).BenchmarkCase.Descriptor.WorkloadMethod);
            Assert.Equal(nameof(BenchmarkEventHandlerBase.OnEndValidationStage), events[3].EventType);
            Assert.Equal(nameof(BenchmarkEventHandlerBase.OnStartBuildStage), events[4].EventType);
        }

        [Fact]
        public void WhenUsingEventHandlerWithUnsupportedBenchmark()
        {
            var toolchain = new AllUnsupportedToolchain();
            var events = RunBenchmarksAndRecordEvents(new[] { typeof(ClassA) }, toolchain: toolchain);

            Assert.Equal(3, events.Count);
            Assert.Equal(nameof(BenchmarkEventHandlerBase.OnStartValidationStage), events[0].EventType);
            Assert.Equal(nameof(BenchmarkEventHandlerBase.OnValidationError), events[1].EventType);
            Assert.Equal(typeof(ClassA).GetMethod(nameof(ClassA.Method1)), (events[1].Args[0] as ValidationError).BenchmarkCase.Descriptor.WorkloadMethod);
            Assert.Equal(nameof(BenchmarkEventHandlerBase.OnValidationError), events[2].EventType);
            Assert.Equal(typeof(ClassA).GetMethod(nameof(ClassA.Method2)), (events[2].Args[0] as ValidationError).BenchmarkCase.Descriptor.WorkloadMethod);
        }

        [Fact]
        public void WhenUsingEventHandlerWithBuildFailures()
        {
            var toolchain = new Toolchain("Build Failure", new AllFailsGenerator(), null, null);
            var events = RunBenchmarksAndRecordEvents(new[] { typeof(ClassA) }, toolchain: toolchain);

            Assert.Equal(10, events.Count);
            Assert.Equal(nameof(BenchmarkEventHandlerBase.OnStartValidationStage), events[0].EventType);
            Assert.Equal(nameof(BenchmarkEventHandlerBase.OnEndValidationStage), events[1].EventType);
            Assert.Equal(nameof(BenchmarkEventHandlerBase.OnStartBuildStage), events[2].EventType);
            Assert.Equal(nameof(BenchmarkEventHandlerBase.OnBuildFailed), events[3].EventType);
            Assert.Equal(typeof(ClassA).GetMethod(nameof(ClassA.Method1)), (events[3].Args[0] as BenchmarkCase).Descriptor.WorkloadMethod);
            Assert.Equal(nameof(BenchmarkEventHandlerBase.OnBuildFailed), events[4].EventType);
            Assert.Equal(typeof(ClassA).GetMethod(nameof(ClassA.Method2)), (events[4].Args[0] as BenchmarkCase).Descriptor.WorkloadMethod);
            Assert.Equal(nameof(BenchmarkEventHandlerBase.OnEndBuildStage), events[5].EventType);
            Assert.Equal(nameof(BenchmarkEventHandlerBase.OnStartRunStage), events[6].EventType);
        }

        private List<LoggingEventHandler.EventData> RunBenchmarksAndRecordEvents(Type[] types, IValidator? validator = null, IToolchain? toolchain = null)
        {
            var eventHandler = new LoggingEventHandler();
            var job = new Job(Job.Dry);
            if (toolchain != null)
                job.Infrastructure.Toolchain = toolchain;

            var config = new ManualConfig()
                .AddJob(job)
                .AddBenchmarkEventHandler(eventHandler)
                .WithOptions(ConfigOptions.DisableOptimizationsValidator)
                .AddExporter(new MockExporter()) // only added to prevent validation warnings about a lack of exporters
                .AddLogger(ConsoleLogger.Default)
                .AddColumnProvider(DefaultColumnProviders.Instance)
                .AddAnalyser(DefaultConfig.Instance.GetAnalysers().ToArray());
            if (validator != null)
                config = config.AddValidator(validator);
            _ = BenchmarkRunner.Run(types, config);
            return eventHandler.Events;
        }

        public class ClassA
        {
            [Benchmark]
            public void Method1() { }
            [Benchmark]
            public void Method2() { }
        }

        public class ClassB
        {
            [Benchmark]
            public void Method1() { }
            [Benchmark]
            public void Method2() { }
            [Benchmark]
            public void Method3() { }
            [Benchmark]
            public void Method4() { }
        }

        public class ClassEmpty { }

        public class ErrorAllCasesValidator : IValidator
        {
            public bool TreatsWarningsAsErrors => true;

            public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters)
            {
                foreach (var benchmark in validationParameters.Benchmarks)
                    yield return new ValidationError(false, "Mock Validation", benchmark);
            }
        }

        public class AllUnsupportedToolchain : Toolchain
        {
            public AllUnsupportedToolchain() : base("AllUnsupported", null, null, null)
            {
            }

            public override IEnumerable<ValidationError> Validate(BenchmarkCase benchmarkCase, IResolver resolver)
            {
                yield return new ValidationError(true, "Unsupported Benchmark", benchmarkCase);
            }
        }

        public class AllFailsGenerator : IGenerator
        {
            public GenerateResult GenerateProject(BuildPartition buildPartition, ILogger logger, string rootArtifactsFolderPath)
            {
                return GenerateResult.Failure(ArtifactsPaths.Empty, new List<string>(), new Exception("Generation Failed"));
            }
        }

        public class LoggingEventHandler : BenchmarkEventHandlerBase
        {
            public class EventData
            {
                public EventData(string eventType, IReadOnlyList<object> args)
                {
                    EventType = eventType;
                    Args = args;
                }

                public string EventType { get; }
                public IReadOnlyList<object> Args { get; }
            }

            public List<EventData> Events { get; } = new List<EventData>();

            public override void OnBuildFailed(BenchmarkCase benchmarkCase, BuildResult buildResult)
            {
                Events.Add(new EventData(nameof(OnBuildFailed), new object[] { benchmarkCase, buildResult }));
            }

            public override void OnEndRunBenchmark(BenchmarkCase benchmarkCase, BenchmarkReport report)
            {
                Events.Add(new EventData(nameof(OnEndRunBenchmark), new object[] { benchmarkCase, report }));
            }

            public override void OnEndRunBenchmarksInType(Type type, Summary summary)
            {
                Events.Add(new EventData(nameof(OnEndRunBenchmarksInType), new object[] { type, summary }));
            }

            public override void OnStartRunBenchmark(BenchmarkCase benchmarkCase)
            {
                Events.Add(new EventData(nameof(OnStartRunBenchmark), new object[] { benchmarkCase }));
            }

            public override void OnStartRunBenchmarksInType(Type type, IReadOnlyList<BenchmarkCase> benchmarks)
            {
                Events.Add(new EventData(nameof(OnStartRunBenchmarksInType), new object[] { type, benchmarks }));
            }

            public override void OnStartBuildStage()
            {
                Events.Add(new EventData(nameof(OnStartBuildStage), new object[] { }));
            }

            public override void OnStartRunStage()
            {
                Events.Add(new EventData(nameof(OnStartRunStage), new object[] { }));
            }

            public override void OnStartValidationStage()
            {
                Events.Add(new EventData(nameof(OnStartValidationStage), new object[] { }));
            }

            public override void OnValidationError(ValidationError validationError)
            {
                Events.Add(new EventData(nameof(OnValidationError), new object[] { validationError }));
            }

            public override void OnEndValidationStage()
            {
                Events.Add(new EventData(nameof(OnEndValidationStage), new object[] { }));
            }

            public override void OnEndBuildStage()
            {
                Events.Add(new EventData(nameof(OnEndBuildStage), new object[] { }));
            }

            public override void OnEndRunStage()
            {
                Events.Add(new EventData(nameof(OnEndRunStage), new object[] { }));
            }
        }
    }
}
