using System;
using System.Linq;

namespace BenchmarkDotNet.Characteristics
{
    internal class CharacteristicHelper
    {
        public const string IdSeparator = ".";

        public static string GetDisplayId(string id)
        {
            return id.Split(new[] { IdSeparator }, StringSplitOptions.RemoveEmptyEntries).Last();
        }
    }
}