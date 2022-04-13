{
   "Title":"BenchmarkDotNet.Samples.IntroSnapshot-20220413-120640",
   "Benchmarks":[
      {
         "FullName":"IntroSnapshot",
         "DisplayInfo":"IntroSnapshot.Sleep: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)",
         "Namespace":"BenchmarkDotNet.Samples",
         "Type":"IntroSnapshot",
         "Method":"Sleep",
         "MethodTitle":"Sleep",
         "Parameters":"",
         "ExecuteResults":[
            {
               "FoundExecutable":true,
               "ExitCode":0,
               "Data":[
                  "","","OverheadJitting  1: 1 op, 213600.00 ns, 213.6000 us/op","WorkloadJitting  1: 1 op, 13981700.00 ns, 13.9817 ms/op","","OverheadJitting  2: 16 op, 210900.00 ns, 13.1813 us/op","WorkloadJitting  2: 16 op, 254823100.00 ns, 15.9264 ms/op","","WorkloadPilot    1: 16 op, 264546300.00 ns, 16.5341 ms/op","WorkloadPilot    2: 32 op, 537420500.00 ns, 16.7944 ms/op","","OverheadWarmup   1: 32 op, 1200.00 ns, 37.5000 ns/op","OverheadWarmup   2: 32 op, 400.00 ns, 12.5000 ns/op","OverheadWarmup   3: 32 op, 300.00 ns, 9.3750 ns/op","OverheadWarmup   4: 32 op, 300.00 ns, 9.3750 ns/op","OverheadWarmup   5: 32 op, 300.00 ns, 9.3750 ns/op","OverheadWarmup   6: 32 op, 400.00 ns, 12.5000 ns/op","OverheadWarmup   7: 32 op, 300.00 ns, 9.3750 ns/op","","OverheadActual   1: 32 op, 400.00 ns, 12.5000 ns/op","OverheadActual   2: 32 op, 500.00 ns, 15.6250 ns/op","OverheadActual   3: 32 op, 300.00 ns, 9.3750 ns/op","OverheadActual   4: 32 op, 400.00 ns, 12.5000 ns/op","OverheadActual   5: 32 op, 300.00 ns, 9.3750 ns/op","OverheadActual   6: 32 op, 300.00 ns, 9.3750 ns/op","OverheadActual   7: 32 op, 400.00 ns, 12.5000 ns/op","OverheadActual   8: 32 op, 400.00 ns, 12.5000 ns/op","OverheadActual   9: 32 op, 400.00 ns, 12.5000 ns/op","OverheadActual  10: 32 op, 300.00 ns, 9.3750 ns/op","OverheadActual  11: 32 op, 300.00 ns, 9.3750 ns/op","OverheadActual  12: 32 op, 300.00 ns, 9.3750 ns/op","OverheadActual  13: 32 op, 300.00 ns, 9.3750 ns/op","OverheadActual  14: 32 op, 300.00 ns, 9.3750 ns/op","OverheadActual  15: 32 op, 300.00 ns, 9.3750 ns/op","OverheadActual  16: 32 op, 300.00 ns, 9.3750 ns/op","OverheadActual  17: 32 op, 300.00 ns, 9.3750 ns/op","OverheadActual  18: 32 op, 300.00 ns, 9.3750 ns/op","OverheadActual  19: 32 op, 700.00 ns, 21.8750 ns/op","OverheadActual  20: 32 op, 400.00 ns, 12.5000 ns/op","","WorkloadWarmup   1: 32 op, 542246900.00 ns, 16.9452 ms/op","WorkloadWarmup   2: 32 op, 537242600.00 ns, 16.7888 ms/op","WorkloadWarmup   3: 32 op, 533025700.00 ns, 16.6571 ms/op","","WorkloadActual   1: 32 op, 523265100.00 ns, 16.3520 ms/op","WorkloadActual   2: 32 op, 565168100.00 ns, 17.6615 ms/op","WorkloadActual   3: 32 op, 557436000.00 ns, 17.4199 ms/op","","WorkloadResult   1: 32 op, 523264800.00 ns, 16.3520 ms/op","WorkloadResult   2: 32 op, 565167800.00 ns, 17.6615 ms/op","WorkloadResult   3: 32 op, 557435700.00 ns, 17.4199 ms/op",""
               ],
               "Errors":[
                  
               ],
               "ExtraOutput":[
                  "// Benchmark Process Environment Information:","// Runtime=.NET Framework 4.8 (4.8.4470.0), X64 RyuJIT","// GC=Concurrent Workstation","// Job: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)"
               ],
               "LunchIndex":1
            }
         ]
      },{
         "FullName":"IntroSnapshot",
         "DisplayInfo":"IntroSnapshot.Thread.Sleep(15): ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)",
         "Namespace":"BenchmarkDotNet.Samples",
         "Type":"IntroSnapshot",
         "Method":"SleepWithDescription",
         "MethodTitle":"Thread.Sleep(15)",
         "Parameters":"",
         "ExecuteResults":[
            {
               "FoundExecutable":true,
               "ExitCode":0,
               "Data":[
                  "","","OverheadJitting  1: 1 op, 203600.00 ns, 203.6000 us/op","WorkloadJitting  1: 1 op, 20763600.00 ns, 20.7636 ms/op","","OverheadJitting  2: 16 op, 192700.00 ns, 12.0438 us/op","WorkloadJitting  2: 16 op, 296813700.00 ns, 18.5509 ms/op","","WorkloadPilot    1: 16 op, 304966800.00 ns, 19.0604 ms/op","WorkloadPilot    2: 32 op, 555954800.00 ns, 17.3736 ms/op","","OverheadWarmup   1: 32 op, 1300.00 ns, 40.6250 ns/op","OverheadWarmup   2: 32 op, 400.00 ns, 12.5000 ns/op","OverheadWarmup   3: 32 op, 300.00 ns, 9.3750 ns/op","OverheadWarmup   4: 32 op, 300.00 ns, 9.3750 ns/op","OverheadWarmup   5: 32 op, 300.00 ns, 9.3750 ns/op","OverheadWarmup   6: 32 op, 300.00 ns, 9.3750 ns/op","OverheadWarmup   7: 32 op, 300.00 ns, 9.3750 ns/op","","OverheadActual   1: 32 op, 300.00 ns, 9.3750 ns/op","OverheadActual   2: 32 op, 300.00 ns, 9.3750 ns/op","OverheadActual   3: 32 op, 300.00 ns, 9.3750 ns/op","OverheadActual   4: 32 op, 400.00 ns, 12.5000 ns/op","OverheadActual   5: 32 op, 300.00 ns, 9.3750 ns/op","OverheadActual   6: 32 op, 300.00 ns, 9.3750 ns/op","OverheadActual   7: 32 op, 300.00 ns, 9.3750 ns/op","OverheadActual   8: 32 op, 300.00 ns, 9.3750 ns/op","OverheadActual   9: 32 op, 300.00 ns, 9.3750 ns/op","OverheadActual  10: 32 op, 400.00 ns, 12.5000 ns/op","OverheadActual  11: 32 op, 300.00 ns, 9.3750 ns/op","OverheadActual  12: 32 op, 300.00 ns, 9.3750 ns/op","OverheadActual  13: 32 op, 300.00 ns, 9.3750 ns/op","OverheadActual  14: 32 op, 300.00 ns, 9.3750 ns/op","OverheadActual  15: 32 op, 300.00 ns, 9.3750 ns/op","","WorkloadWarmup   1: 32 op, 529131000.00 ns, 16.5353 ms/op","WorkloadWarmup   2: 32 op, 570889100.00 ns, 17.8403 ms/op","WorkloadWarmup   3: 32 op, 592679300.00 ns, 18.5212 ms/op","","WorkloadActual   1: 32 op, 559760600.00 ns, 17.4925 ms/op","WorkloadActual   2: 32 op, 544505800.00 ns, 17.0158 ms/op","WorkloadActual   3: 32 op, 565748300.00 ns, 17.6796 ms/op","","WorkloadResult   1: 32 op, 559760300.00 ns, 17.4925 ms/op","WorkloadResult   2: 32 op, 544505500.00 ns, 17.0158 ms/op","WorkloadResult   3: 32 op, 565748000.00 ns, 17.6796 ms/op",""
               ],
               "Errors":[
                  
               ],
               "ExtraOutput":[
                  "// Benchmark Process Environment Information:","// Runtime=.NET Framework 4.8 (4.8.4470.0), X64 RyuJIT","// GC=Concurrent Workstation","// Job: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)"
               ],
               "LunchIndex":1
            }
         ]
      }
   ]
}