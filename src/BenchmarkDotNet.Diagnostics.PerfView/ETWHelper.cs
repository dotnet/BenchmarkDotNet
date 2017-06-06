using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Diagnostics.Tracing.Stacks;

namespace BenchmarkDotNet.Diagnostics.PerfView
{
    public static class ETWHelper
    {
        /// <summary>
        /// Transforms ETL file to ETLX and returns TraceLog instance
        /// </summary>
        public static (TraceLog log, string etlxFile) GetTraceLog(string fileName)
        {
            string etlxFile = Path.ChangeExtension(fileName, ".etlx");
            // workaround: This was sooo slow with attached debugger because lots of trace was produced
            var li = System.Diagnostics.Trace.Listeners.Cast<System.Diagnostics.TraceListener>().ToArray();
            for (int i = 0; i < System.Diagnostics.Trace.Listeners.Count; i++)
            {
                System.Diagnostics.Trace.Listeners.RemoveAt(0);
            }
            // only resolve symbols for currently loaded assemblies, don't waste time with others
            var dllWhiteList = new HashSet<string>(AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetName().Name), StringComparer.OrdinalIgnoreCase);
            var log = TraceLog.OpenOrConvert(fileName, new TraceLogOptions { ShouldResolveSymbols = name => dllWhiteList.Contains(name) });
            System.Diagnostics.Trace.Listeners.AddRange(li);
            return (log, etlxFile);
        }

        public static Dictionary<string, CallTreeItem> GetCallTree(StackSource stacks, out float timePerStack)
        {
            var callTree = new Dictionary<string, CallTreeItem>(StringComparer.OrdinalIgnoreCase);
            var currentStack = new HashSet<string>();
            void AddRecursively(StackSourceCallStackIndex index, int depth = 0, CallTreeItem parent = null)
            {
                var name = stacks.GetFrameName(stacks.GetFrameIndex(index), false);
                if (name == "BROKEN") return;
                var isRecursion = !currentStack.Add(name);
                var caller = stacks.GetCallerIndex(index);
                if (!callTree.TryGetValue(name, out var item)) callTree.Add(name, item = new CallTreeItem(name));
                if (!isRecursion) item.IncSamples++;
                if (depth == 0) item.Samples++;
                else item.AddCallee(parent);
                parent?.AddCaller(item);

                if (caller != StackSourceCallStackIndex.Invalid) AddRecursively(caller, depth + 1, item);
                if (!isRecursion) currentStack.Remove(name);
            }
            var metric = float.NaN;
            stacks.ForEach(stack => {
                if (float.IsNaN(metric)) metric = stack.Metric;
                if (metric != stack.Metric) throw new Exception();
                if (stack.Count != 1) throw new Exception();
                AddRecursively(stack.StackIndex);
                AddRecursively(stack.StackIndex);
            });
            timePerStack = metric;
            return callTree;
        }

        /// <summary>
        /// Gets inclusive time fractions spent in specified methods. First method is a baseline, all results are methodTime[i] / baselineTime. You probably want to use your main function as a baseline to filter out noise and only could percents of your benchmark run.
        /// </summary>
        public static IEnumerable<float> ComputeTimeFractions(Dictionary<string, CallTreeItem> callTree, string[] methodNames)
        {
            CallTreeItem FindMethod(string name)
            {
                if (callTree.TryGetValue(name, out var result)) return result;
                else return callTree.FirstOrDefault(n => n.Key.StartsWith(name + "(", StringComparison.OrdinalIgnoreCase)).Value;
            }

            if (!(FindMethod(methodNames.First()) is CallTreeItem baseLine))
            {
            // baseline not found
                foreach (var f in methodNames)
                    yield return 0f;
                yield break;
            }
            while (baseLine.Callers.Count == 1)
                baseLine = baseLine.Callers.Single().Key;

            foreach (var m in methodNames)
            {
                if (FindMethod(m) is CallTreeItem cti)
                {
                    yield return (float)cti.IncSamples / (float)baseLine.IncSamples;
                }
                else
                    yield return 0f;
            }
        }


        public class CallTreeItem
        {
            public CallTreeItem(string name)
            {
                this.Name = name;
            }

            public string Name { get; }
            public ulong Samples { get; set; }
            public ulong IncSamples { get; set; }
            private Dictionary<CallTreeItem, int> _callees = null;
            public IEnumerable<CallTreeItem> Callees => _callees?.Keys ?? Enumerable.Empty<CallTreeItem>();
            public IEnumerable<KeyValuePair<CallTreeItem, int>> CalleesWithSampleCounts => _callees ?? Enumerable.Empty<KeyValuePair<CallTreeItem, int>>();
            public void AddCallee(CallTreeItem cti)
            {
                (_callees ?? (_callees = new Dictionary<CallTreeItem, int>(2))).TryGetValue(cti, out int current);
                _callees[cti] = current + 1;
            }

            public Dictionary<CallTreeItem, int> Callers { get; } = new Dictionary<CallTreeItem, int>(2);
            public void AddCaller(CallTreeItem cti)
            {
                Callers.TryGetValue(cti, out int current);
                Callers[cti] = current + 1;
            }

        }
    }
}
