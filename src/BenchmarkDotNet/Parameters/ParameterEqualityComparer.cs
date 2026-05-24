namespace BenchmarkDotNet.Parameters;

internal class ParameterEqualityComparer : IEqualityComparer<ParameterInstances>
{
    public static readonly ParameterEqualityComparer Instance = new();

    public bool Equals(ParameterInstances? x, ParameterInstances? y)
    {
        if (ReferenceEquals(x, y))
            return true;

        if (x is null || y is null)
            return false;

        if (x.Count != y.Count)
            return false;

        for (int i = 0; i < x.Count; i++)
        {
            if (!DeepEqualityComparer.Instance.Equals(x[i]?.Value, y[i]?.Value))
            {
                return false;
            }
        }

        return true;
    }

    public int GetHashCode(ParameterInstances obj)
        => obj?.ValueInfo.GetHashCode() ?? 0;
}