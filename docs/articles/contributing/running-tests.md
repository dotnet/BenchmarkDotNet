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

## Running tests on GitHub Actions with GitHub CLI

This section describes how to run tests on GitHub Actions by using CLI.

### Prerequisites

The following tools are required to run tests on CI with `gh workflow run` command.

* [GitHub CLI](https://cli.github.com)

Run `gh auth login` command to authenticate with your GitHub account after installation.

### Common parameters of `gh workflow run` command

Following parameters need to specified to invoke workflow with `gh workflow run` command.

| name              | description
|-------------------|----------------------
| `<workflow-name>` | Specify workflow name (e.g. `run-tests-selected.yaml`)
| `--repo`          | Specify target repository name with `{owner}/{repo}` format (e.g. `dotnet/BenchmarkDotNet`)
| `--ref`           | Specify target branch name

> [!NOTE]
> `gh run workflow` command requires `Write` permission on target repository.
> Contributors who don't have write permission to `dotnet/BenchmarkDotNet` need to run workflow on a **forked** repository.

### Parameters of `run-tests-selected.yaml` workflow

`run-tests-selected.yaml` accept following input parameters. (Passed with `-f` or --field)

| name                          | default                                  | description
|-------------------------------|------------------------------------------|---------------------
| `runs_on`                     | `ubuntu-latest`                          | GitHub Actions runner image name (`windows-latest` `windows-11-arm` `ubuntu-latest` `macos-latest` `macos-15-intel`)
| `project`                     | `tests/BenchmarkDotNet.IntegrationTests` | Specify path of project directory
| `framework`                   | `net8.0`                                 | Target Framework (e.g. `net8.0`, `net472`)
| `filter`                      | `BenchmarkDotNet`                        | Test filter text (It's used for `dotnet test --filter`) Use default value when running all tests
| `iteration_count`             | `1`                                      | Count of test loop (It's expected to be used for flaky tests)
| `skip_setup_additional_tools` | `false`                                  | Skip setup additional tools for Wasm/NativeAOT tests  

### Command examples

#### Run `CompletedWorkItemCountIsAccurate` test

```pwsh
gh workflow run run-tests-selected.yaml --repo dotnet/BenchmarkDotNet --ref master -f runs_on=windows-latest -f filter=CompletedWorkItemCountIsAccurate
```
