using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Parameters;
using BenchmarkDotNet.Validators;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains.InProcess.NoEmit
{
    /// <summary>
    /// In-process (no emit) toolchain runner
    /// </summary>
    internal class InProcessNoEmitRunner
    {
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Runnable))]
        public static async ValueTask<int> Run(IHost host, ExecuteParameters parameters)
        {
            // the first thing to do is to let diagnosers hook in before anything happens
            // so all jit-related diagnosers can catch first jit compilation!
            host.BeforeAnythingElse();

            try
            {
                // we are not using Runnable here in any direct way in order to avoid strong dependency Main<=>Runnable
                // which could cause the jitting/assembly loading to happen before we do anything
                // we have some jitting diagnosers and we want them to catch all the informations!!

                string inProcessRunnableTypeName = $"{typeof(InProcessNoEmitRunner).FullName}+{nameof(Runnable)}";
                var type = typeof(InProcessNoEmitRunner).GetTypeInfo().Assembly.GetType(inProcessRunnableTypeName)
                    ?? throw new InvalidOperationException($"Bug: type {inProcessRunnableTypeName} not found.");

                var methodInfo = type.GetMethod(nameof(Runnable.RunCore), BindingFlags.Public | BindingFlags.Static)
                    ?? throw new InvalidOperationException($"Bug: method {nameof(Runnable.RunCore)} in {inProcessRunnableTypeName} not found.");
                await (ValueTask) methodInfo.Invoke(null, [host, parameters])!;

                return 0;
            }
            catch (Exception oom) when (oom is OutOfMemoryException || oom is TargetInvocationException reflection && reflection.InnerException is OutOfMemoryException)
            {
                host.WriteLine();
                host.WriteLine("OutOfMemoryException!");
                host.WriteLine("BenchmarkDotNet continues to run additional iterations until desired accuracy level is achieved. It's possible only if the benchmark method doesn't have any side-effects.");
                host.WriteLine("If your benchmark allocates memory and keeps it alive, you are creating a memory leak.");
                host.WriteLine("You should redesign your benchmark and remove the side-effects. You can use `OperationsPerInvoke`, `IterationSetup` and `IterationCleanup` to do that.");
                host.WriteLine();
                host.WriteLine(oom.ToString());

                return -1;
            }
            catch (Exception ex)
            {
                host.WriteLine();
                host.WriteLine(ex.ToString());
                return -1;
            }
            finally
            {
                host.AfterAll();
            }
        }

        /// <summary>Fills the properties of the instance of the object used to run the benchmark.</summary>
        /// <param name="instance">The instance.</param>
        /// <param name="benchmarkCase">The benchmark.</param>
        internal static void FillMembers(object instance, BenchmarkCase benchmarkCase)
        {
            foreach (var parameter in benchmarkCase.Parameters.Items)
            {
                var flags = BindingFlags.Public;
                flags |= parameter.IsStatic ? BindingFlags.Static : BindingFlags.Instance;

                var targetType = benchmarkCase.Descriptor.Type;
                var paramProperty = targetType.GetProperty(parameter.Name, flags);

                if (paramProperty == null)
                {
                    var paramField = targetType.GetField(parameter.Name, flags);
                    if (paramField == null)
                        throw new InvalidOperationException(
                            $"Type {targetType.FullName}: no property or field {parameter.Name} found.");

                    var callInstance = paramField.IsStatic ? null : instance;
                    paramField.SetValue(callInstance, parameter.Value);
                }
                else
                {
                    var setter = paramProperty.GetSetMethod();
                    if (setter == null)
                        throw new InvalidOperationException(
                            $"Type {targetType.FullName}: no settable property {parameter.Name} found.");

                    var callInstance = setter.IsStatic ? null : instance;
                    setter.Invoke(callInstance, new[] { parameter.Value });
                }
            }
        }

        [UsedImplicitly]
        private static class Runnable
        {
            public static async ValueTask RunCore(IHost host, ExecuteParameters parameters)
            {
                var benchmarkCase = parameters.BenchmarkCase;
                var target = benchmarkCase.Descriptor;
                var job = new Job().Apply(benchmarkCase.Job).Freeze();
                int unrollFactor = benchmarkCase.Job.ResolveValue(RunMode.UnrollFactorCharacteristic, EnvironmentResolver.Instance);

                // DONTTOUCH: these should be allocated together
                var instance = Activator.CreateInstance(benchmarkCase.Descriptor.Type)!;
                var workloadAction = BenchmarkActionFactory.CreateWorkload(target, instance, unrollFactor);
                var overheadAction = BenchmarkActionFactory.CreateOverhead(target, instance, unrollFactor);
                var globalSetupAction = BenchmarkActionFactory.CreateGlobalSetup(target, instance);
                var globalCleanupAction = BenchmarkActionFactory.CreateGlobalCleanup(target, instance);
                var iterationSetupAction = BenchmarkActionFactory.CreateIterationSetup(target, instance);
                var iterationCleanupAction = BenchmarkActionFactory.CreateIterationCleanup(target, instance);

                FillMembers(instance, benchmarkCase);

                host.WriteLine();
                foreach (string infoLine in BenchmarkEnvironmentInfo.GetCurrent().ToFormattedString())
                    host.WriteLine("// {0}", infoLine);
                host.WriteLine("// Job: {0}", job.DisplayInfo);
                host.WriteLine();

                var errors = BenchmarkProcessValidator.Validate(job, instance);
                if (await ValidationErrorReporter.ReportIfAnyAsync(errors, host))
                    return;

                var compositeInProcessDiagnoserHandler = new Diagnosers.CompositeInProcessDiagnoserHandler(
                    parameters.CompositeInProcessDiagnoser.InProcessDiagnosers
                        .Select((d, i) => Diagnosers.InProcessDiagnoserRouter.Create(d, benchmarkCase, i))
                        .Where(r => r.handler != null)
                        .ToArray(),
                    host,
                    parameters.DiagnoserRunMode,
                    new Diagnosers.InProcessDiagnoserActionArgs(instance)
                );
                if (parameters.DiagnoserRunMode == Diagnosers.RunMode.SeparateLogic)
                {
                    await compositeInProcessDiagnoserHandler.HandleAsync(BenchmarkSignal.SeparateLogic);
                    return;
                }
                await compositeInProcessDiagnoserHandler.HandleAsync(BenchmarkSignal.BeforeEngine);

                var engineParameters = new EngineParameters
                {
                    Host = host,
                    WorkloadActionNoUnroll = workloadAction.InvokeNoUnroll,
                    WorkloadActionUnroll = workloadAction.InvokeUnroll,
                    OverheadActionNoUnroll = overheadAction.InvokeNoUnroll,
                    OverheadActionUnroll = overheadAction.InvokeUnroll,
                    GlobalSetupAction = globalSetupAction.InvokeSingle,
                    GlobalCleanupAction = () =>
                    {
                        workloadAction.Complete();
                        overheadAction.Complete();
                        return globalCleanupAction.InvokeSingle();
                    },
                    IterationSetupAction = iterationSetupAction.InvokeSingle,
                    IterationCleanupAction = iterationCleanupAction.InvokeSingle,
                    TargetJob = job,
                    OperationsPerInvoke = target.OperationsPerInvoke,
                    RunExtraIteration = benchmarkCase.Config.HasExtraIterationDiagnoser(benchmarkCase),
                    BenchmarkName = FullNameProvider.GetBenchmarkName(benchmarkCase),
                    InProcessDiagnoserHandler = compositeInProcessDiagnoserHandler
                };

                var results = await job
                    .ResolveValue(InfrastructureMode.EngineFactoryCharacteristic, InfrastructureResolver.Instance)!
                    .Create(engineParameters)
                    .RunAsync();
                host.ReportResults(results); // printing costs memory, do this after runs

                await compositeInProcessDiagnoserHandler.HandleAsync(BenchmarkSignal.AfterEngine);
            }
        }
    }
}
