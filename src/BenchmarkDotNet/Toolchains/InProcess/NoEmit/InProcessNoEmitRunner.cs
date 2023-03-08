using System;
using System.Reflection;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains.InProcess.NoEmit
{
    /// <summary>
    /// In-process (no emit) toolchain runner
    /// </summary>
    internal class InProcessNoEmitRunner
    {
        public static int Run(IHost host, BenchmarkCase benchmarkCase)
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
                methodInfo.Invoke(null, new object[] { host, benchmarkCase });

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
            public static void RunCore(IHost host, BenchmarkCase benchmarkCase)
            {
                var target = benchmarkCase.Descriptor;
                var job = benchmarkCase.Job; // TODO: filter job (same as SourceCodePresenter does)?
                int unrollFactor = benchmarkCase.Job.ResolveValue(RunMode.UnrollFactorCharacteristic, EnvironmentResolver.Instance);

                // DONTTOUCH: these should be allocated together
                var instance = Activator.CreateInstance(benchmarkCase.Descriptor.Type);
                var workloadAction = BenchmarkActionFactory.CreateWorkload(target, instance, unrollFactor);
                var overheadAction = BenchmarkActionFactory.CreateOverhead(target, instance, unrollFactor);
                var globalSetupAction = BenchmarkActionFactory.CreateGlobalSetup(target, instance);
                var globalCleanupAction = BenchmarkActionFactory.CreateGlobalCleanup(target, instance);
                var iterationSetupAction = BenchmarkActionFactory.CreateIterationSetup(target, instance);
                var iterationCleanupAction = BenchmarkActionFactory.CreateIterationCleanup(target, instance);
                var dummy1 = BenchmarkActionFactory.CreateDummy();
                var dummy2 = BenchmarkActionFactory.CreateDummy();
                var dummy3 = BenchmarkActionFactory.CreateDummy();

                FillMembers(instance, benchmarkCase);

                host.WriteLine();
                foreach (string infoLine in BenchmarkEnvironmentInfo.GetCurrent().ToFormattedString())
                    host.WriteLine("// {0}", infoLine);
                host.WriteLine("// Job: {0}", job.DisplayInfo);
                host.WriteLine();

                var engineParameters = new EngineParameters
                {
                    Host = host,
                    WorkloadActionNoUnroll = invocationCount =>
                    {
                        for (int i = 0; i < invocationCount; i++)
                            workloadAction.InvokeSingle();
                    },
                    WorkloadActionUnroll = workloadAction.InvokeMultiple,
                    Dummy1Action = dummy1.InvokeSingle,
                    Dummy2Action = dummy2.InvokeSingle,
                    Dummy3Action = dummy3.InvokeSingle,
                    OverheadActionNoUnroll = invocationCount =>
                    {
                        for (int i = 0; i < invocationCount; i++)
                            overheadAction.InvokeSingle();
                    },
                    OverheadActionUnroll = overheadAction.InvokeMultiple,
                    GlobalSetupAction = globalSetupAction.InvokeSingle,
                    GlobalCleanupAction = globalCleanupAction.InvokeSingle,
                    IterationSetupAction = iterationSetupAction.InvokeSingle,
                    IterationCleanupAction = iterationCleanupAction.InvokeSingle,
                    TargetJob = job,
                    OperationsPerInvoke = target.OperationsPerInvoke,
                    MeasureExtraStats = benchmarkCase.Config.HasExtraStatsDiagnoser(),
                    BenchmarkName = FullNameProvider.GetBenchmarkName(benchmarkCase)
                };

                using (var engine = job
                    .ResolveValue(InfrastructureMode.EngineFactoryCharacteristic, InfrastructureResolver.Instance)
                    .CreateReadyToRun(engineParameters))
                {
                    var results = engine.Run();

                    host.ReportResults(results); // printing costs memory, do this after runs
                }
            }
        }
    }
}