extern alias PerfView;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Diagnostics.Tracing.Session;
using Microsoft.Diagnostics.Tracing.Stacks;


namespace BenchmarkDotNet.Diagnostics.PerfView
{
    public class PerfViewHandler
    {
        private static object locker = new object();
        private static AppDomain perfViewDomain;

        public static AppDomain GetPerfViewDomain()
        {
            if (perfViewDomain != null) return perfViewDomain;
            lock (locker)
            {
                if (perfViewDomain != null) return perfViewDomain;
                return perfViewDomain = CreatePerfViewDomain();
            }
        }

        static AppDomain CreatePerfViewDomain()
        {
            var d = AppDomain.CreateDomain("PerfViewDomain");
            d.DoCallBack(PerfViewDomainLaunch);
            return d;
        }


        static void StopCollection(int collectionHandle, bool rundown)
        {
            var d = GetPerfViewDomain();
            Console.WriteLine($"Stopping ETW collection, rundown: {rundown}");
            lock (locker)
            {
                d.SetData("collectionHandle", collectionHandle);
                d.SetData("doRundown", rundown);
                d.DoCallBack(PerfViewDomainMethods.StopCollection);
            }
        }
        static void MergeAndZip(string fileName)
        {
            var d = GetPerfViewDomain();
            lock (locker)
            {
                d.SetData("fileName", fileName);
                d.DoCallBack(PerfViewDomainMethods.MergeFile);
            }
        }

        public static CollectionHandler StartCollection(string outFile, Process process)
        {
            if (outFile.EndsWith(".zip")) outFile = outFile.Remove(outFile.Length - 4);
            var d = GetPerfViewDomain();
            lock (locker)
            {
                d.SetData("outFile", outFile);
                d.SetData("processName", process?.ProcessName);

                d.DoCallBack(PerfViewDomainMethods.RunCollection);

                var handle = (int)d.GetData("collectionHandle");
                return new CollectionHandler(handle, process, outFile);
            }
        }

        public class CollectionHandler
        {
            private readonly int collectionHandle;
            private readonly Process process;
            private readonly string outFile;

            public CollectionHandler(int collectionHandle, Process process, string outFile)
            {
                this.collectionHandle = collectionHandle;
                this.process = process;
                this.outFile = Path.GetFullPath(outFile);
            }
            public Func<Dictionary<string, ETWHelper.CallTreeItem>> StopAndLazyMerge(bool leaveTraces)
            {
                bool rundown = !this.process.WaitForExit(500);
                StopCollection(collectionHandle, rundown: rundown);
                var pid = process.Id;

                return () => {
                    var tmpFile = outFile + ".merged.etl";
                    if (File.Exists(tmpFile)) File.Delete(tmpFile);

                    var allFiles = GetAllResultFiles(outFile);
                    if (allFiles.Length > 1)
                    {
                        TraceEventSession.Merge(allFiles, tmpFile, TraceEventMergeOptions.Compress);
                        File.Delete(outFile);
                        File.Move(tmpFile, outFile);
                    }
                    else if (allFiles[0] != outFile)
                        File.Move(allFiles[0], outFile);
                    var (etlx, callTree) = TryGetCallTree(pid);

                    if (!leaveTraces)
                    {
                        if (File.Exists(etlx)) File.Delete(etlx);
                        File.Delete(outFile);
                        foreach (var f in Directory.GetFiles(Path.GetDirectoryName(outFile)))
                        {
                            if (Path.GetFileName(f).StartsWith(Path.GetFileNameWithoutExtension(outFile)))
                            {
                                File.Delete(f);
                            }
                        }
                    }
                    else if (rundown)
                    {

                        Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(outFile), "symbols"));
                        MergeAndZip(outFile);
                    }
                    else
                    {
                        Zip(outFile);
                    }
                    return callTree;
                };
            }

            private (string etlx, Dictionary<string, ETWHelper.CallTreeItem> callTree) TryGetCallTree(int pid)
            {
                string maybeEtlx = null;
                try
                {
                    var (tlog, etlx) = ETWHelper.GetTraceLog(outFile);
                    maybeEtlx = etlx;
                    using (tlog)
                    {
                        var proc = tlog.Processes.FirstOrDefault(p => p.ProcessID == pid);
                        var stacks = new TraceEventStackSource(proc.EventsInProcess.Filter(t => t is SampledProfileTraceData));
                        var callTree = ETWHelper.GetCallTree(stacks, out var timeModifier);
                        //if (GetAllResultFiles(outFile) is var allFiles && allFiles.Length > 1)
                        //{
                        //    string tempName = Path.ChangeExtension(outFile, ".etl.new");
                        //    TraceEventSession.Merge(allFiles, tempName);
                        //    // Delete the originals.  
                        //    foreach (var mergeInput in allFiles)
                        //        File.Delete(mergeInput);
                        //    // Place the output in its final resting place.  
                        //    File.Move(tempName, outFile);
                        //}
                        return (etlx, callTree);
                    }
                }
                catch(Exception ex)
                {
                    return (maybeEtlx, new Dictionary<string, ETWHelper.CallTreeItem>());
                }
            }


            public static void Zip(string fileName)
            {
                var etlWriter = new ZippedETLWriter(fileName);
                etlWriter.Zip = true;
                etlWriter.CompressETL = true;
                etlWriter.DeleteInputFile = true;
                etlWriter.WriteArchive();
            }

            static string[] GetAllResultFiles(string etlFileName)
            {
                var dir = Path.GetDirectoryName(etlFileName);
                if (dir.Length == 0)
                    dir = ".";
                var baseName = Path.GetFileNameWithoutExtension(etlFileName);
                List<string> mergeInputs = new List<string>();
                mergeInputs.Add(etlFileName);
                mergeInputs.AddRange(Directory.GetFiles(dir, baseName + ".kernel*.etl"));
                mergeInputs.AddRange(Directory.GetFiles(dir, baseName + ".clr*.etl"));
                mergeInputs.AddRange(Directory.GetFiles(dir, baseName + ".user*.etl"));
                return mergeInputs.ToArray();
            }
        }



        public static void PerfViewDomainLaunch()
        {
            PerfView::PerfView.App.Unpack();
            if (!PerfView::PerfView.App.IsElevated) throw new Exception("Application must be elevated.");
        }
    }
}
