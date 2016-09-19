using System;
using System.Linq;

namespace BenchmarkDotNet.Characteristics
{
    internal class CharacteristicHelper
    {
        public const string IdSeparator = ".";
        private static readonly string[] IdSeparatorArray = { IdSeparator };

        public static string GetDisplayName(string id)
        {
            return id.Split(IdSeparatorArray, StringSplitOptions.RemoveEmptyEntries).Last();
        }
    }
}