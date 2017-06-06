extern alias PerfView;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using PerfView::PerfView;

namespace BenchmarkDotNet.Diagnostics.PerfView
{
    static class PerfViewDomainMethods
    {
        static CommandLineArgs CreateArgs(string outFile, string processName = null)
        {
            var a = App.CommandLineArgs = new CommandLineArgs();
            a.ParseArgs(new[] { "/NoGui"/*, "/KernelEvents:None"*/ });
            a.RestartingToElevelate = ""; // this should prevent PerfView from trying to elevate itself
            a.Zip = false;
            a.Merge = false;
            a.InMemoryCircularBuffer = false;
            a.DotNetAllocSampled = false;
            a.CpuSampleMSec = 1f; // 0.125f;
            //a.StackCompression = true;

            a.DataFile = outFile;
            //a.Process = processName ?? a.Process;
            a.NoNGenRundown = true;
            a.NoV2Rundown = true;
            a.NoRundown = true;
            a.NoClrRundown = true;
            a.TrustPdbs = true;
            a.UnsafePDBMatch = true;
            return a;
        }

        private static ConcurrentDictionary<int, (CommandProcessor, CommandLineArgs)> collectionHandles = new ConcurrentDictionary<int, (CommandProcessor, CommandLineArgs)>();
        private static int collectionHandleIdCtr;
        public static void RunCollection()
        {
            var path = (string)AppDomain.CurrentDomain.GetData("outFile");
            var processName = (string)AppDomain.CurrentDomain.GetData("processName");

            var commandProcessor = App.CommandProcessor = new CommandProcessor() { LogFile = Console.Out };
            var commandArgs = CreateArgs(path, processName);
            commandProcessor.Start(commandArgs);

            var handle = Interlocked.Increment(ref collectionHandleIdCtr);
            collectionHandles[handle] = (commandProcessor, commandArgs);
            AppDomain.CurrentDomain.SetData("collectionHandle", handle);
        }

        public static void StopCollection()
        {
            var handle = (int)AppDomain.CurrentDomain.GetData("collectionHandle");
            var rundown = (bool)AppDomain.CurrentDomain.GetData("doRundown");
            var (proc, args) = collectionHandles[handle];
            if (rundown)
            {
                args.NoRundown = args.NoNGenRundown = args.NoClrRundown = false;
            }
            collectionHandles.TryRemove(handle, out var _);
            proc.Stop(args);
        }

        public static void MergeFile()
        {
            var file = (string)AppDomain.CurrentDomain.GetData("fileName");
            var args = CreateArgs(file);
            args.Zip = true;
            args.Merge = false; // file is already merged into one
            var processor = new CommandProcessor { LogFile = Console.Out };
            processor.Merge(args);
        }
    }
}
