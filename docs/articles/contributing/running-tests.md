# Running Tests

* To run "Classic" tests build the solution and run runClassicTests.cmd in the tests directory or comment out the `netcoreapp1.0` part of all project.json files 
that belong to the testing projects.
* To run "Core" tests you just need to open Test Explorer in Visual Studio and rebuild the solution. Then tests show up in Test Explorer and you can simply run them.

@alert Important
Remember to do both before pulling a PR or publishing new version
@end

* For some unit tests (e.g. for exporter tests) BenchmarkDotNet uses [approval tests'](http://approvaltests.sourceforge.net/) implementation for .NET: [ApprovalTests.Net](https://github.com/approvals/ApprovalTests.Net).
	* The expected value for each test is stored in a `*.approved.txt` file located near the test source file in the repository. ApprovalTests.NET generates approved file's names automatically according test name and its parameters. This files must be added under the source control. 
	* It also creates a `*.received` file for each failed test. You can use different reporters for convenient file comparison. By default we use XUnit2Reporter, so you can find test run results on the test runner console as usual. You can add [UseReporter(typeof(KDiffReporter))] on test class and then ApprovalTests will open KDiff for each failed test. This way you can easily understand what's the difference between approved and received values and choose the correct one.