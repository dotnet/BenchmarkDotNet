using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Helpers
{
    internal static class StringHelper
    {
        [NotNull]
        public static Dictionary<string, string> Parse([CanBeNull] string content, char separator)
        {
            var values = new Dictionary<string, string>();
            var list = content?.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (list != null)
                foreach (string line in list)
                    if (line.IndexOf(separator) != -1)
                    {
                        var lineParts = line.Split(separator);
                        if (lineParts.Length >= 2)
                            values[lineParts[0].Trim()] = lineParts[1].Trim();
                    }
            return values;
        }
        
        [NotNull]
        public static List<Dictionary<string, string>> ParseList([CanBeNull] string content, char separator)
        {
            var items = new List<Dictionary<string, string>>();
            Dictionary<string, string> units = null;

            var list = Regex.Split(content ?? "", "\r?\n");
            foreach (string line in list)
                if (line.IndexOf(separator) != -1)
                {
                    var lineParts = line.Split(separator);
                    if (lineParts.Length >= 2)
                    {
                        if (units == null)
                            items.Add(units = new Dictionary<string, string>());
                        units[lineParts[0].Trim()] = lineParts[1].Trim();
                    }
                }
                else
                    units = null;
            return items;
        }
    }
}