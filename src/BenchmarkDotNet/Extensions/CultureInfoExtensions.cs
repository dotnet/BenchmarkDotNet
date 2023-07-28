using System.Globalization;
using BenchmarkDotNet.Helpers;

namespace BenchmarkDotNet.Extensions
{
    internal static class CultureInfoExtensions
    {
        public static string GetActualListSeparator(this CultureInfo? cultureInfo)
        {
            cultureInfo = cultureInfo ?? DefaultCultureInfo.Instance;
            string listSeparator = cultureInfo.TextInfo.ListSeparator;

            // On .NET Core + Linux, TextInfo.ListSeparator returns NumberFormat.NumberGroupSeparator
            // To workaround this behavior, we patch empty ListSeparator with ";"
            // See also: https://github.com/dotnet/runtime/issues/536
            if (string.IsNullOrWhiteSpace(listSeparator))
                listSeparator = ";";

            return listSeparator;
        }
    }
}