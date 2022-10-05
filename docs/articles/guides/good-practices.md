# Good Practices

## Use the Release build without an attached debugger

Never use the Debug build for benchmarking. *Never*. The debug version of the target method can run 10–100 times slower. 
The release mode means that you should have `<Optimize>true</Optimize>` in your csproj file 
or use [/optimize](https://learn.microsoft.com/dotnet/csharp/language-reference/compiler-options/) for `csc`. Also your never 
should use an attached debugger (e.g. Visual Studio or WinDbg) during the benchmarking. The best way is 
build our benchmark in the Release mode and run it from the command line.

## Try different environments

Please, don't extrapolate your results. Or do it very carefully.
I remind you again: the results in different environments may vary significantly. If a `Foo1` method is faster than 
a `Foo2` method for CLR4, .NET Framework 4.5, x64, RyuJIT, Windows, it means that the `Foo1` method is faster than 
the `Foo2` method for CLR4, .NET Framework 4.5, x64, RyuJIT, Windows and nothing else. And you can not say anything 
about methods performance for CLR 2 or .NET Framework 4.6 or LegacyJIT-x64 or x86 or Linux+Mono until you try it. 

## Avoid dead code elimination

You should also use the result of calculation. For example, if you run the following code:

```cs
void Foo()
{
    Math.Exp(1);
}
```

then JIT can eliminate this code because the result of `Math.Exp` is not used. The better way is use it like this:

```cs
double Foo()
{
    return Math.Exp(1);
}
```

## Power settings and other applications

* Turn off all of the applications except the benchmark process and the standard OS processes. If you run benchmark and work in the Visual Studio at the same time, it can negatively affect to benchmark results.
* If you use laptop for benchmarking, keep it plugged in and use the maximum performance mode.

