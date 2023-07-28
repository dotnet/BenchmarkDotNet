using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Extensions;

namespace BenchmarkDotNet.Helpers
{
    internal static class LinuxOsReleaseHelper
    {
        public static string? GetNameByOsRelease(string[] lines)
        {
            try
            {
                var values = new Dictionary<string, string>();
                foreach (string line in lines)
                {
                    string[] parts = line.Split(new[] {'='}, 2);

                    if (parts.Length == 2)
                    {
                        string key = parts[0].Trim();
                        string value = parts[1].Trim();

                        // remove quotes if value is quoted
                        if (value.Length >= 2 &&
                            ((value.StartsWith("\"") && value.EndsWith("\"")) ||
                             (value.StartsWith("'") && value.EndsWith("'"))))
                        {
                            value = value.Substring(1, value.Length - 2);
                        }

                        values[key] = value;
                    }
                }

                string? id = values.GetValueOrDefault("ID");
                string? name = values.GetValueOrDefault("NAME");
                string? version = values.GetValueOrDefault("VERSION");

                string[] idsWithExtendedVersion = { "ubuntu", "linuxmint", "solus", "kali" };
                if (idsWithExtendedVersion.Contains(id) && !string.IsNullOrEmpty(version) && !string.IsNullOrEmpty(name))
                    return name + " " + version;

                string? prettyName = values.GetValueOrDefault("PRETTY_NAME");
                if (prettyName != null)
                    return prettyName;

                if (name != null && version != null)
                    return name + " " + version;

                if (name != null)
                    return name;

                if (id != null)
                    return id;

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}