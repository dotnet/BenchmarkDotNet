using BenchmarkDotNet.Attributes;

public struct MyStruct
{
    public long A, B;
}

public enum MyEnum { First, Second }

public class ReturnValues
{
    private int value = 42;

    [Benchmark] public void Void() => value++;
    [Benchmark] public int Int() => value + 1;
    [Benchmark] public double Double() => value * 1.5;
    [Benchmark] public string String() => "text";
    [Benchmark] public Guid Guid() => System.Guid.Empty;
    [Benchmark] public int[] Array() => [value];
    [Benchmark] public int? NullableInt() => value;
    [Benchmark] public ref int RefInt() => ref value;
}

// Return types the generated registration has to provide.
public class CustomStructs
{
    private MyStruct field;
    private string text = "text";

    [Benchmark] public MyStruct Struct() => new() { A = 1, B = 2 };
    [Benchmark] public ref MyStruct RefStruct() => ref field;
    [Benchmark] public ref readonly MyStruct RefReadonlyStruct() => ref field;
    [Benchmark] public ref string RefString() => ref text;
    [Benchmark] public (int, long) Tuple() => (1, 2);
    [Benchmark] public MyEnum Enum() => MyEnum.Second;
}

public class AsyncResults
{
    [Benchmark] public Task Task() => System.Threading.Tasks.Task.CompletedTask;
    [Benchmark] public Task<int> TaskOfInt() => System.Threading.Tasks.Task.FromResult(1);
    [Benchmark] public async Task<string> AsyncString() { await System.Threading.Tasks.Task.Yield(); return "text"; }
    [Benchmark] public ValueTask ValueTask() => default;
    [Benchmark] public ValueTask<int> ValueTaskOfInt() => new(1);
    [Benchmark] public ValueTask<string> ValueTaskOfString() => new("text");
    [Benchmark] public ValueTask<MyStruct> ValueTaskOfStruct() => new(new MyStruct());

    [Benchmark]
    public async IAsyncEnumerable<int> AsyncEnumerableOfInt()
    {
        await System.Threading.Tasks.Task.Yield();
        yield return 1;
    }

    [Benchmark]
    public async IAsyncEnumerable<MyStruct> AsyncEnumerableOfStruct()
    {
        await System.Threading.Tasks.Task.Yield();
        yield return new MyStruct();
    }
}

public class Parameters
{
    [Params(1, 2)] public int Number;
    [ParamsAllValues] public bool Flag;

    public IEnumerable<string> Names() => ["a", "bb"];
    [ParamsSource(nameof(Names))] public string Name = "";

    [Benchmark] public int Sum() => Number + Name.Length + (Flag ? 1 : 0);
}

public class SetupAndCleanup
{
    private int value;

    [GlobalSetup] public Task GlobalSetup() { value = 1; return Task.CompletedTask; }
    [GlobalCleanup] public void GlobalCleanup() => value = 0;
    [IterationSetup] public void IterationSetup() => value++;
    [IterationCleanup] public void IterationCleanup() => value--;
    [Benchmark] public int Read() => value;
}

public abstract class BaseBenchmarks
{
    [Benchmark] public MyStruct Inherited() => new() { A = 3 };
}

public class Derived : BaseBenchmarks;

[GenericTypeArguments(typeof(int))]
[GenericTypeArguments(typeof(string))]
[GenericTypeArguments(typeof(MyStruct))]
public class Generic<T>
{
    [Benchmark] public T? Default() => default;
}

[MemoryDiagnoser]
public class Memory
{
    [Benchmark] public byte[] Allocate() => new byte[64];
}
