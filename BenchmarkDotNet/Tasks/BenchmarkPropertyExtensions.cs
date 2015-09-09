using System.Collections.Generic;
using System.Linq;

namespace BenchmarkDotNet.Tasks
{
    public static class BenchmarkPropertyExtensions
    {
        public static string GetValue(this IEnumerable<BenchmarkProperty> properties, string name)
            => properties.FirstOrDefault(p => p.Name == name)?.Value ?? "";

        public static IEnumerable<string> GetAllNames(this IEnumerable<BenchmarkProperty> properties)
            => properties.Select(p => p.Name).Distinct();

        public static IList<string> GetImportantNames(
            this IEnumerable<IEnumerable<BenchmarkProperty>> propertiesEnumerable)
        {
            var allProperties = new List<BenchmarkProperty>();
            foreach (var properties in propertiesEnumerable)
                allProperties.AddRange(properties);
            var allNames = allProperties.GetAllNames();
            return (
                from name in allNames
                let targetProperties = allProperties.Where(p => p.Name == name)
                let targetValues = targetProperties.Select(p => p.Value).Distinct().ToArray()
                where targetValues.Length > 1
                select name).ToList();
        }
    }
}