using BenchmarkDotNet.Extensions;

namespace BenchmarkDotNet.Parameters;

// Renders an array parameter's value for the Params column, e.g. "Int32[3]" or "Int32[2,2][]".
internal static class ArrayDisplay
{
    public static string GetDisplayString(Array array)
    {
        string dimensionRepr = string.Join(", ", Enumerable.Range(0, array.Rank).Select(array.GetLength));

        var (baseElementTypeRepr, innerDimensions) = GetDisplayString(array.GetType());

        innerDimensions = string.Join("", innerDimensions.Split([']'], count: 2).Skip(1));

        return $"{baseElementTypeRepr}[{dimensionRepr}]{innerDimensions}";
    }

    private static (string BaseElementTypeRepr, string InnerDimensions) GetDisplayString(Type arrayType)
    {
        var elemType = arrayType.GetElementType()!;

        if (elemType.IsArray)
        {
            var (baseElementTypeRepr, innerDimensions) = GetDisplayString(elemType);

            return (baseElementTypeRepr, $"[{new string(',', arrayType.GetArrayRank() - 1)}]{innerDimensions}");
        }

        return (elemType.GetDisplayName(), $"[{new string(',', arrayType.GetArrayRank() - 1)}]");
    }
}
