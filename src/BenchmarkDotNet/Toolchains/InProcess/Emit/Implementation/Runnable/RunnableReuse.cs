using System.Linq;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Parameters;
using BenchmarkDotNet.Validators;
using static BenchmarkDotNet.Toolchains.InProcess.Emit.Implementation.RunnableConstants;
using static BenchmarkDotNet.Toolchains.InProcess.Emit.Implementation.RunnableReflectionHelpers;

namespace BenchmarkDotNet.Toolchains.InProcess.Emit.Implementation
{
    public static class RunnableReuse
    {
        public static (Job, EngineParameters, IEngineFactory) PrepareForRun<T>(T instance, IHost host, ExecuteParameters parameters)
        {
            var benchmarkCase = parameters.BenchmarkCase;
            FillObjectMembers(instance, benchmarkCase);

            DumpEnvironment(host);

            var job = CreateJob(benchmarkCase);
            DumpJob(host, job);

            var errors = BenchmarkEnvironmentInfo.Validate(job);
            if (ValidationErrorReporter.ReportIfAny(errors, host))
                return default;

            var compositeInProcessDiagnoserHandler = new CompositeInProcessDiagnoserHandler(
                    parameters.CompositeInProcessDiagnoser.InProcessDiagnosers
                        .Select((d, i) => new InProcessDiagnoserRouter()
                        {
                            index = i,
                            runMode = d.GetRunMode(benchmarkCase),
                            handler = InProcessDiagnoserRouter.CreateOrNull(d.GetHandlerData(benchmarkCase))
                        })
                        .Where(r => r.handler != null)
                        .ToArray(),
                    host,
                    parameters.DiagnoserRunMode,
                    new InProcessDiagnoserActionArgs(instance)
                );
            if (parameters.DiagnoserRunMode == Diagnosers.RunMode.SeparateLogic)
            {
                compositeInProcessDiagnoserHandler.Handle(BenchmarkSignal.SeparateLogic);
                return default;
            }
            compositeInProcessDiagnoserHandler.Handle(BenchmarkSignal.BeforeEngine);

            var engineParameters = CreateEngineParameters(instance, benchmarkCase, host, compositeInProcessDiagnoserHandler);
            var engineFactory = GetEngineFactory(benchmarkCase);

            return (job, engineParameters, engineFactory);
        }

        public static void FillObjectMembers<T>(T instance, BenchmarkCase benchmarkCase)
        {
            var argIndex = 0;
            foreach (var argInfo in benchmarkCase.Descriptor.WorkloadMethod.GetParameters())
            {
                SetArgumentField(instance, benchmarkCase, argInfo, argIndex);
                argIndex++;
            }

            foreach (var paramInfo in benchmarkCase.Parameters.Items
                .Where(parameter => !parameter.IsArgument))
            {
                SetParameter(instance, paramInfo);
            }
        }

        private static void DumpEnvironment(IHost host)
        {
            host.WriteLine();
            foreach (var infoLine in BenchmarkEnvironmentInfo.GetCurrent().ToFormattedString())
            {
                host.WriteLine("// {0}", infoLine);
            }
        }

        private static Job CreateJob(BenchmarkCase benchmarkCase)
        {
            var job = new Job();
            job.Apply(benchmarkCase.Job);
            job.Freeze();
            return job;
        }

        private static void DumpJob(IHost host, Job job)
        {
            host.WriteLine("// Job: {0}", job.DisplayInfo);
            host.WriteLine();
        }

        private static IEngineFactory GetEngineFactory(BenchmarkCase benchmarkCase)
        {
            return benchmarkCase.Job.ResolveValue(
                InfrastructureMode.EngineFactoryCharacteristic,
                InfrastructureResolver.Instance);
        }

        private static EngineParameters CreateEngineParameters<T>(T instance, BenchmarkCase benchmarkCase, IHost host, CompositeInProcessDiagnoserHandler inProcessDiagnoserHandler)
            => new()
            {
                Host = host,
                WorkloadActionUnroll = LoopCallbackFromMethod(instance, WorkloadActionUnrollMethodName),
                WorkloadActionNoUnroll = LoopCallbackFromMethod(instance, WorkloadActionNoUnrollMethodName),
                Dummy1Action = CallbackFromMethod(instance, Dummy1MethodName),
                Dummy2Action = CallbackFromMethod(instance, Dummy2MethodName),
                Dummy3Action = CallbackFromMethod(instance, Dummy3MethodName),
                OverheadActionNoUnroll = LoopCallbackFromMethod(instance, OverheadActionNoUnrollMethodName),
                OverheadActionUnroll = LoopCallbackFromMethod(instance, OverheadActionUnrollMethodName),
                GlobalSetupAction = CallbackFromField(instance, GlobalSetupActionFieldName),
                GlobalCleanupAction = CallbackFromField(instance, GlobalCleanupActionFieldName),
                IterationSetupAction = CallbackFromField(instance, IterationSetupActionFieldName),
                IterationCleanupAction = CallbackFromField(instance, IterationCleanupActionFieldName),
                TargetJob = benchmarkCase.Job,
                OperationsPerInvoke = benchmarkCase.Descriptor.OperationsPerInvoke,
                MeasureExtraStats = benchmarkCase.Config.HasExtraStatsDiagnoser(),
                BenchmarkName = FullNameProvider.GetBenchmarkName(benchmarkCase),
                InProcessDiagnoserHandler = inProcessDiagnoserHandler
            };
    }
}