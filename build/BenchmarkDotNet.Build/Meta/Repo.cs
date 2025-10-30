using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Build.Meta;

public static class Repo
{
    public const string Owner = "dotnet";
    public const string Name = "BenchmarkDotNet";
    private const string HttpsUrlBase = $"https://github.com/{Owner}/{Name}";
    public const string SshGitUrl = $"git@github.com:{Owner}/{Name}.git";
    
    public const string ChangelogBranch = "docs-changelog";
    public const string DocsStableBranch = "docs-stable";
    public const string MasterBranch = "master";

    public const string MaintainerAuthorName = "Andrey Akinshin <andrey.akinshin@gmail.com>";
    public const string MaintainerAuthorEmail = "andrey.akinshin@gmail.com";
    
    public static async Task<int> GetDependentProjectsNumber()
    {
        using var httpClient = new HttpClient();
        const string url = $"{HttpsUrlBase}/network/dependents";
        var response = await httpClient.GetAsync(new Uri(url));
        var dependentsPage = await response.Content.ReadAsStringAsync();
        var match = new Regex(@"([0-9\,]+)[\n\r\s]+Repositories").Match(dependentsPage);
        var number = int.Parse(match.Groups[1].Value.Replace(",", ""));
        number = number / 100 * 100;
        return number;
    }

}