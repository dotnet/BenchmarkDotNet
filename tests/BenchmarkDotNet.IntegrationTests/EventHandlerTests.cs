using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.EventHandlers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Tests.Validators;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.Results;
using BenchmarkDotNet.Validators;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers.MicrosoftWindowsTCPIP;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace BenchmarkDotNet.IntegrationTests
{
    public class EventHandlerTests
    {
        [Fact]
        public void WhenUsingEventHandlerAndNoBenchmarks()
        {
            var events = RunBenchmarksAndRecordEvents(new[] { typeof(ClassEmpty) });
            Assert.Single(events);
            Assert.Equal(nameof(IEventHandler.HandleStartValidationStage), events[0].EventType);
        }

        [Fact]
        public void WhenUsingEventHandlerOnSingleClass()
        {
            var events = RunBenchmarksAndRecordEvents(new[] { typeof(ClassA) });

            Assert.Equal(9, events.Count);

            Assert.Equal(nameof(IEventHandler.HandleStartValidationStage), events[0].EventType);
            Assert.Equal(nameof(IEventHandler.HandleStartBuildStage), events[1].EventType);
            Assert.Equal(nameof(IEventHandler.HandleStartRunStage), events[2].EventType);

            var benchmarkTypeAndMethods = new List<(Type Type, string[] MethodNames)>
            {
                (typeof(ClassA), new[]{ nameof(ClassA.Method1), nameof(ClassA.Method2) })
            };

            int eventIndex = 3;
            foreach ((var type, var methodNames) in benchmarkTypeAndMethods)
            {
                Assert.Equal(nameof(IEventHandler.HandleRunBenchmarksInType), events[eventIndex].EventType);
                Assert.Equal(type, events[eventIndex++].Args[0] as Type);

                foreach (var method in methodNames)
                {
                    var methodDescriptor = type.GetMethod(method);
                    Assert.Equal(nameof(IEventHandler.HandleRunBenchmark), events[eventIndex].EventType);
                    Assert.Equal(methodDescriptor, (events[eventIndex++].Args[0] as BenchmarkCase).Descriptor.WorkloadMethod);

                    Assert.Equal(nameof(IEventHandler.HandleCompletedBenchmark), events[eventIndex].EventType);
                    Assert.Equal(methodDescriptor, (events[eventIndex++].Args[0] as BenchmarkCase).Descriptor.WorkloadMethod);
                }

                Assert.Equal(nameof(IEventHandler.HandleCompletedBenchmarksInType), events[eventIndex].EventType);
                Assert.Equal(type, events[eventIndex++].Args[0] as Type);
            }
        }

        [Fact]
        public void WhenUsingEventHandlerOnMultipleClasses()
        {
            var events = RunBenchmarksAndRecordEvents(new[] { typeof(ClassA), typeof(ClassB) });

            Assert.Equal(19, events.Count);

            Assert.Equal(nameof(IEventHandler.HandleStartValidationStage), events[0].EventType);
            Assert.Equal(nameof(IEventHandler.HandleStartBuildStage), events[1].EventType);
            Assert.Equal(nameof(IEventHandler.HandleStartRunStage), events[2].EventType);

            var benchmarkTypeAndMethods = new List<(Type Type, string[] MethodNames)>
            {
                (typeof(ClassA), new[]{ nameof(ClassA.Method1), nameof(ClassA.Method2) }),
                (typeof(ClassB), new[]{ nameof(ClassB.Method1), nameof(ClassB.Method2), nameof(ClassB.Method3), nameof(ClassB.Method4) })
            };

            int eventIndex = 3;
            foreach ((var type, var methodNames) in benchmarkTypeAndMethods)
            {
                Assert.Equal(nameof(IEventHandler.HandleRunBenchmarksInType), events[eventIndex].EventType);
                Assert.Equal(type, events[eventIndex++].Args[0] as Type);

                foreach (var method in methodNames)
                {
                    var methodDescriptor = type.GetMethod(method);
                    Assert.Equal(nameof(IEventHandler.HandleRunBenchmark), events[eventIndex].EventType);
                    Assert.Equal(methodDescriptor, (events[eventIndex++].Args[0] as BenchmarkCase).Descriptor.WorkloadMethod);

                    Assert.Equal(nameof(IEventHandler.HandleCompletedBenchmark), events[eventIndex].EventType);
                    Assert.Equal(methodDescriptor, (events[eventIndex++].Args[0] as BenchmarkCase).Descriptor.WorkloadMethod);
                }

                Assert.Equal(nameof(IEventHandler.HandleCompletedBenchmarksInType), events[eventIndex].EventType);
                Assert.Equal(type, events[eventIndex++].Args[0] as Type);
            }
        }

        [Fact]
        public void WhenUsingEventHandlerWithValidationErrors()
        {
            var validator = new ErrorAllCasesValidator();
            var events = RunBenchmarksAndRecordEvents(new[] { typeof(ClassA) }, validator);

            Assert.Equal(11, events.Count);
            Assert.Equal(nameof(IEventHandler.HandleStartValidationStage), events[0].EventType);
            Assert.Equal(nameof(IEventHandler.HandleValidationError), events[1].EventType);
            Assert.Equal(typeof(ClassA).GetMethod(nameof(ClassA.Method1)), (events[1].Args[0] as ValidationError).BenchmarkCase.Descriptor.WorkloadMethod);
            Assert.Equal(nameof(IEventHandler.HandleValidationError), events[2].EventType);
            Assert.Equal(typeof(ClassA).GetMethod(nameof(ClassA.Method2)), (events[2].Args[0] as ValidationError).BenchmarkCase.Descriptor.WorkloadMethod);
            Assert.Equal(nameof(IEventHandler.HandleStartBuildStage), events[3].EventType);
        }

        [Fact]
        public void WhenUsingEventHandlerWithUnsupportedBenchmark()
        {
            var toolchain = new AllUnsupportedToolchain();
            var events = RunBenchmarksAndRecordEvents(new[] { typeof(ClassA) }, toolchain: toolchain);

            Assert.Equal(5, events.Count);
            Assert.Equal(nameof(IEventHandler.HandleStartValidationStage), events[0].EventType);
            Assert.Equal(nameof(IEventHandler.HandleUnsupportedBenchmark), events[1].EventType);
            Assert.Equal(typeof(ClassA).GetMethod(nameof(ClassA.Method1)), (events[1].Args[0] as BenchmarkCase).Descriptor.WorkloadMethod);
            Assert.Equal(nameof(IEventHandler.HandleUnsupportedBenchmark), events[2].EventType);
            Assert.Equal(typeof(ClassA).GetMethod(nameof(ClassA.Method2)), (events[2].Args[0] as BenchmarkCase).Descriptor.WorkloadMethod);
            Assert.Equal(nameof(IEventHandler.HandleValidationError), events[3].EventType);
            Assert.Equal(typeof(ClassA).GetMethod(nameof(ClassA.Method1)), (events[3].Args[0] as ValidationError).BenchmarkCase.Descriptor.WorkloadMethod);
            Assert.Equal(nameof(IEventHandler.HandleValidationError), events[4].EventType);
            Assert.Equal(typeof(ClassA).GetMethod(nameof(ClassA.Method2)), (events[4].Args[0] as ValidationError).BenchmarkCase.Descriptor.WorkloadMethod);
        }

        [Fact]
        public void WhenUsingEventHandlerWithBuildFailures()
        {
            var toolchain = new Toolchain("Build Failure", new AllFailsGenerator(), null, null);
            var events = RunBenchmarksAndRecordEvents(new[] { typeof(ClassA) }, toolchain: toolchain);

            Assert.Equal(7, events.Count);
            Assert.Equal(nameof(IEventHandler.HandleStartValidationStage), events[0].EventType);
            Assert.Equal(nameof(IEventHandler.HandleStartBuildStage), events[1].EventType);
            Assert.Equal(nameof(IEventHandler.HandleBuildFailed), events[2].EventType);
            Assert.Equal(typeof(ClassA).GetMethod(nameof(ClassA.Method1)), (events[2].Args[0] as BenchmarkCase).Descriptor.WorkloadMethod);
            Assert.Equal(nameof(IEventHandler.HandleBuildFailed), events[3].EventType);
            Assert.Equal(typeof(ClassA).GetMethod(nameof(ClassA.Method2)), (events[3].Args[0] as BenchmarkCase).Descriptor.WorkloadMethod);
            Assert.Equal(nameof(IEventHandler.HandleStartRunStage), events[4].EventType);
        }

        private List<LoggingEventHandler.EventData> RunBenchmarksAndRecordEvents(Type[] types, IValidator? validator = null, IToolchain? toolchain = null)
        {
            var eventHandler = new LoggingEventHandler();
            var job = new Job(Job.Dry);
            if (toolchain != null)
                job.Infrastructure.Toolchain = toolchain;

            var config = new ManualConfig()
                .AddJob(job)
                .AddEventHandler(eventHandler)
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

        public class LoggingEventHandler : IEventHandler
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

            public void HandleBuildFailed(BenchmarkCase benchmarkCase, BuildResult buildResult)
            {
                Events.Add(new EventData(nameof(HandleBuildFailed), new object[] { benchmarkCase, buildResult }));
            }

            public void HandleCompletedBenchmark(BenchmarkCase benchmarkCase, BenchmarkReport report)
            {
                Events.Add(new EventData(nameof(HandleCompletedBenchmark), new object[] { benchmarkCase, report }));
            }

            public void HandleCompletedBenchmarksInType(Type type, Summary summary)
            {
                Events.Add(new EventData(nameof(HandleCompletedBenchmarksInType), new object[] { type, summary }));
            }

            public void HandleRunBenchmark(BenchmarkCase benchmarkCase)
            {
                Events.Add(new EventData(nameof(HandleRunBenchmark), new object[] { benchmarkCase }));
            }

            public void HandleRunBenchmarksInType(Type type, IReadOnlyList<BenchmarkCase> benchmarks)
            {
                Events.Add(new EventData(nameof(HandleRunBenchmarksInType), new object[] { type, benchmarks }));
            }

            public void HandleStartBuildStage()
            {
                Events.Add(new EventData(nameof(HandleStartBuildStage), new object[] { }));
            }

            public void HandleStartRunStage()
            {
                Events.Add(new EventData(nameof(HandleStartRunStage), new object[] { }));
            }

            public void HandleStartValidationStage()
            {
                Events.Add(new EventData(nameof(HandleStartValidationStage), new object[] { }));
            }

            public void HandleUnsupportedBenchmark(BenchmarkCase benchmarkCase)
            {
                Events.Add(new EventData(nameof(HandleUnsupportedBenchmark), new object[] { benchmarkCase }));
            }

            public void HandleValidationError(ValidationError validationError)
            {
                Events.Add(new EventData(nameof(HandleValidationError), new object[] { validationError }));
            }
        }
    }
}
