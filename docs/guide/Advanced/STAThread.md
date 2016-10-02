# STAThread

If the code you want to benchmark requires `[System.STAThread]` then you need to apply this attribute to the benchmarked method. BenchmarkDotNet will generate executable with `[STAThread]` applied to it's `Main` method.

## Example

```cs
[Benchmark, System.STAThread]
public void CheckForSTA()
{
    if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
    {
        throw new ThreadStateException("The current threads apartment state is not STA");
    }
}
```
