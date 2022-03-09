using System;
using System.Collections.Generic;
using System.Linq;

namespace BenchmarkDotNet.Order
{
    internal class CategoryComparer : IComparer<string[]>
    {
        private const string Separator = "§";
        public static readonly CategoryComparer Instance = new ();

        public int Compare(string[] x, string[] y)
        {
            return string.Compare(GetUniqueId(x), GetUniqueId(y), StringComparison.Ordinal);
        }

        private static string GetUniqueId(string[] categories)
        {
            var list = categories.ToList();
            list.Sort();
            return string.Join(Separator, categories);
        }
    }
}