# Setup

If you have some data that you want to initialize, the `[Setup]` method is the best place for this. It will be invoked only once before each iteration.

## Example

```cs
private int[] initialValuesArray;
private List<int> initialValuesList;

[Setup]
public void SetupData()
{
    int MaxCounter = 1000;
    initialValuesArray = Enumerable.Range(0, MaxCounter).ToArray();
    initialValuesList = Enumerable.Range(0, MaxCounter).ToList();
}

[Benchmark]
public int ForLoop()
{
    var counter = 0;
    for (int i = 0; i < initialValuesArray.Length; i++)
        counter += initialValuesArray[i];
    return counter;
}

[Benchmark]
public int ForEachList()
{
    var counter = 0;
    foreach (var i in initialValuesList)
        counter += i;
    return counter;
}

```
