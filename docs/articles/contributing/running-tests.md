# Running Tests

To run all tests just run the following command in the repo root:

```cmd
dotnet test -c Release BenchmarkDotNet.sln
```

Most of the tests projects target `net461` and `net5.0`. If the change that you want to test is not specific to any particular runtime, you can run the tests for one of them.

```cmd
dotnet test -c Release -f net5.0 BenchmarkDotNet.sln
```

You should be able to run all of tests from your IDE as well.

## Verify Tests

For some unit tests (e.g. for exporter tests) BenchmarkDotNet uses [Verify](https://github.com/VerifyTests/Verify).
* The expected value for each test is stored in a `*.verified.txt` file located near the test source file in the repository. Verify generates verified file's names automatically according test name and its parameters. This files must be added under the source control.
* It also creates a `*.received` file for each failed test. You can use diff tools for convenient file comparison. By default you can find test run results on the test runner console as usual. You can comment out the line ```result.DisableDiff()``` in ```VerifySettingsFactory.Create``` method and then Verify will open KDiff for each failed test. This way you can easily understand what's the difference between verified and received values and choose the correct one.
