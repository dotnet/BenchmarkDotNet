using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Engines
{
    internal abstract class EngineStage(IterationStage stage, IterationMode mode)
    {
        internal readonly IterationStage Stage = stage;
        internal readonly IterationMode Mode = mode;

        internal abstract List<Measurement> GetMeasurementList();
        internal abstract bool GetShouldRunIteration(List<Measurement> measurements, ref long invokeCount);

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static IEnumerable<EngineStage> EnumerateStages(IEngine engine, RunStrategy strategy, bool evaluateOverhead)
        {
            // It might be possible to add the jitting stage to this, but it's done in EngineFactory.CreateReadyToRun for now.

            if (strategy != RunStrategy.ColdStart)
            {
                if (strategy != RunStrategy.Monitoring)
                {
                    var pilotStage = EnginePilotStage.GetStage(engine);
                    if (pilotStage != null)
                    {
                        yield return pilotStage;
                    }

                    if (evaluateOverhead)
                    {
                        yield return EngineWarmupStage.GetOverhead();
                        yield return EngineActualStage.GetOverhead(engine);
                    }
                }

                yield return EngineWarmupStage.GetWorkload(engine, strategy);
            }

            yield return EngineActualStage.GetWorkload(engine, strategy);
        }
    }
}