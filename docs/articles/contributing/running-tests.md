# Running Tests

To run all tests just run the following command in the repo root:

```cmd
dotnet test -c Release BenchmarkDotNet.slnx
```

Most of the tests projects target `net472` and `net8.0`. If the change that you want to test is not specific to any particular runtime, you can run the tests for one of them.

```cmd
dotnet test -c Release -f net8.0 BenchmarkDotNet.slnx
```

You should be able to run all of tests from your IDE as well.

## Verify Tests

For some unit tests (e.g. for exporter tests) BenchmarkDotNet uses [Verify](https://github.com/VerifyTests/Verify).

* The expected value for each test is stored in a `*.verified.txt` file located near the test source file in the repository. Verify generates verified file's names automatically according test name and its parameters. This files must be added under the source control.
* It also creates a `*.received` file for each failed test. You can use diff tools for convenient file comparison. By default you can find test run results on the test runner console as usual. You can comment out the line ```result.DisableDiff()``` in ```VerifySettingsFactory.Create``` method and then Verify will open KDiff for each failed test. This way you can easily understand what's the difference between verified and received values and choose the correct one.

## Running tests on GitHub Actions

This section describes how to run specific tests on GitHub Actions.

### Prerequisites

The following tools are required to run tests on CI.

* [Git](https://git-scm.com)
* [GitHub CLI](https://cli.github.com)
* [PowerShell 7](https://learn.microsoft.com/en-us/powershell/scripting/install/install-powershell)

> [!NOTE]
> `gh run workflow` command requires `Write` permission on target repository.
> Contributors who don't have write permission need to run workflow on a **forked** repository.

This section's scripts are expected to be executed with PowerShell 7(`pwsh`)

### How to run specific tests with `gh workflow run` command

**1.Get target repository information**
Run the following script to gets variables that are used by command.

```pwsh
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true
Set-StrictMode -Version

# Gets target repository with `owner/repo` format.
$originUrl = git config --get remote.origin.url
$repo = if ($originUrl -match 'github\.com[:/](.+?)/(.+?)(?:\.git)?$') {"$($Matches[1])/$($Matches[2])"} else { throw 'Failed to get repository "owner/repo" from $originUrl' }

# Gets current branch name.
$branchName = git branch --show-current
```

**2. Set target tests information**
Modify following scripts and execute script to set target tests information.

```pwsh
$arguments = @(
  '--repo', $repo
  '--ref', $branchName
  '-f', 'runs_on=windows-latest'                         # `windows-latest`, `windows-11-arm`, `ubuntu-latest`, `ubuntu-24.04-arm`, `macos-latest`, `macos-15-intel`
  '-f', 'project=tests/BenchmarkDotNet.IntegrationTests' # Target test project path.
  '-f', 'framework=net8.0'                               # `net8.0` or `net472`
  '-f', 'filter=*.WasmTests.*'                           # Specify filter with GlobFilter syntax(https://benchmarkdotnet.org/articles/configs/filters.html)
)
```

**3. Start tests on CI**
Run following command to start tests on CI.

```pwsh
gh workflow run run-tests-selected.yaml @arguments
```
