# Rules of benchmarking

##Use the Release build without an attached debugger

Never use the Debug build for benchmarking. *Never*. The debug version of the target method can run 10–100 times slower. 
The release mode means that you should have `<Optimize>true</Optimize>` in your csproj file 
or use [/optimize](https://msdn.microsoft.com/en-us/library/t0hfscdc.aspx) for `csc`. Also your never 
should use an attached debugger (e.g. Visual Studio or WinDbg) during the benchmarking. The best way is 
build our benchmark in the Release mode and run it from the command line.

##Try different environments

Please, don't extrapolate your results. Or do it very carefully.
I remind you again: the results in different environments may vary significantly. If a `Foo1` method is faster than 
a `Foo2` method for CLR4, .NET Framework 4.5, x64, RyuJIT, Windows, it means that the `Foo1` method is faster than 
the `Foo2` method for CLR4, .NET Framework 4.5, x64, RyuJIT, Windows and nothing else. And you can not say anything 
about methods performance for CLR 2 or .NET Framework 4.6 or LegacyJIT-x64 or x86 or Linux+Mono until you try it. 

##Avoid dead code elimination

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

##Minimize work with memory

If you don't measure efficiency of access to memory, efficiency of the CPU cache, efficiency of GC, you 
shouldn't create big arrays and you shouldn't allocate big amount of memory. For example, you want to 
measure performance of `ConvertAll(x => 2 * x).ToList()`. You can write code like this:

```cs
List<int> list = /* ??? */;
public List<int> ConvertAll()
{
    return list.ConvertAll(x => 2 * x).ToList();
}
```

In this case, you should create a small list like this:

```cs
List<int> list = new List<int> { 1, 2, 3, 4, 5 };
```

If you create a big list (with millions of elements), then you will also measure efficiency of the CPU cache 
because you will have big amount of [cache miss](http://en.wikipedia.org/wiki/CPU_cache#Cache_miss) during the calculation.  

##Power settings and other applications

* Turn off all of the applications except the benchmark process and the standard OS processes. If you run benchmark and work in the Visual Studio at the same time, it can negatively affect to benchmark results.
* If you use laptop for benchmarking, keep it plugged in and use the maximum performance mode.

