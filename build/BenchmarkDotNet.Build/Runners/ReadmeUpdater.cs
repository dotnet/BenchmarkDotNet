using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BenchmarkDotNet.Build.Meta;
using Cake.FileHelpers;

namespace BenchmarkDotNet.Build.Runners;

public class ReadmeUpdater
{
    public static void Run(BuildContext context) => new ReadmeUpdater().RunInternal(context);

    private void RunInternal(BuildContext context)
    {
        var dependentProjectsNumber = GetDependentProjectsNumber().Result;
        var updaters = new LineUpdater[]
        {
            new(
                "The library is adopted by",
                @"\[(\d+)\+ GitHub projects\]",
                dependentProjectsNumber
            ),
            new(
                "BenchmarkDotNet is already adopted by more than ",
                @"\[(\d+)\+\]",
                dependentProjectsNumber
            ),
        };

        var file = context.RootDirectory.CombineWithFilePath("README.md");
        var lines = context.FileReadLines(file);
        for (var i = 0; i < lines.Length; i++)
        {
            foreach (var updater in updaters)
                lines[i] = updater.Apply(lines[i]);
        }

        context.FileWriteLines(file, lines);
    }

    private static async Task<int> GetDependentProjectsNumber()
    {
        using var httpClient = new HttpClient();
        const string url = $"{Repo.HttpsUrlBase}/network/dependents";
        var response = await httpClient.GetAsync(new Uri(url));
        var dependentsPage = await response.Content.ReadAsStringAsync();
        var match = new Regex(@"([0-9\,]+)[\n\r\s]+Repositories").Match(dependentsPage);
        var number = int.Parse(match.Groups[1].Value.Replace(",", ""));
        number = number / 100 * 100;
        return number;
    }

    private class LineUpdater
    {
        public string Prefix { get; }
        public Regex Regex { get; }
        public int Value { get; }

        public LineUpdater(string prefix, string regex, int value)
        {
            Prefix = prefix;
            Regex = new Regex(regex);
            Value = value;
        }

        public string Apply(string line)
        {
            if (!line.StartsWith(Prefix))
                return line;

            var match = Regex.Match(line);
            if (!match.Success)
                return line;

            // Groups[1] refers to the first group (\d+)
            var numberString = match.Groups[1].Value;
            var number = int.Parse(numberString);
            return line.Replace(number.ToString(), Value.ToString());
        }
    }
}