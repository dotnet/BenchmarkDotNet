# Contributing to Disassembler

The disassembler might looks scarry, but once you know how it works and how to debug it, it's very easy to develop it.

### How it works

We have 3 disassemblers:

- Mono
- x64 for Windows and Linux
- x86 for Windows

The MonoDisassembler is very simple: it spawns Mono with the right arguments to get the asm, Mono prints the output to the console and we just parse it. Single class does the job: `MonoDisassembler`.

When it comes to Windows disassemblers it's not so easy. To obtain the disassm we are using ClrMD. ClrMD can attach only to the process of same bitness (architecture).
This is why we have two disassemblers: x64 and x86. The code is the same (single class, linked in two projects) but compiled for two different architectures. We keep both disassemblers in the resources of the BenchmarkDotNet.dll. When we need the disassembler, we search for it in the resources, copy it to the disk and run (it's an exe).

On Linux it's simpler (only x64 is supported) and we don't spawn a new process (everything is done in-proc).

### How to debug the disassembler

You need to create a new console app project which executes the code that you would like to disassemble. In this app, you need to run the desired code (to get it jitted) and just don't exit before attaching the disassembler and getting the disassembly.

Disassembler requires some arguments to run: id of the process to attach, full type name of the type which contains desired method, name of desired method and few other (see the example below).

Personally I use following code to run the console app and print arguments that are required to attach to it:

```cs
namespace Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            var result = Benchmark(); // execute the benchmark do method gets jitted

            Console.WriteLine(
            $"{Process.GetCurrentProcess().Id} " +   // process Id
                $"\"{typeof(Program).FullName}\" " + // full type name
                $"{nameof(Benchmark)} " +            // benchmarked method name
                $"{bool.FalseString} " +             // print Source
                "2 " +                               // recursive depth
                $"{Path.GetTempFileName()}.xml");    // result xml file path

            while(true)
            {
                Console.WriteLine("Press Ctrl+C to kill the process");
                Console.ReadLine(); // block the exe, attach with Disassembler now
            }

            GC.KeepAlive(result);
        }

        public static IntPtr Benchmark()
        {
            return new IntPtr(42).Multiply(4);
        }
    }

    public static class IntPtrHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static IntPtr Multiply(this IntPtr a, int factor)
        {
            return (sizeof(IntPtr) == sizeof(int))
                ? new IntPtr((int)a * factor)
                : new IntPtr((long)a * factor);
        }
    }
}
```

**Important**: Please remember that every new classic .NET project in VS compiles as 32 bit. If you want to check the asm produced for x64 you need to go to the properties of the console app (Alt+Enter) and uncheck "Prefer 32 bit" in the "Build" tab.

Once you configure your app, you should run it. It will give you an output similar to this:

`13672 Sample.Program Benchmark True 7 C:\Users\adsitnik\AppData\Local\Temp\tmpDCB9.tmp.xml`

Now you go to BenchmarkDotNet solution, select desired Disassembler project in the Solution Explorer and Set it as Startup project. After this you go to the project's properties and in the Debug tab copy-paste the arguments for the disassembler. Now when you start debugging, your IDE will spawn new process of the disassembler with the right arguments to attach to the desired exe. You should be able to debug it like any other app.

Please keep in mind that you should always use the disassembler for the correct processor architecture. If you fail to debug it, you are most probably using the wrong one.
