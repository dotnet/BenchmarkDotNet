# Running Tests

* To run "Classic" tests build the solution and run runClassicTests.cmd in the tests directory or comment out the `netcoreapp1.0` part of all project.json files 
that belong to the testing projects.
* To run "Core" tests you just need to open Test Explorer in Visual Studio and rebuild the solution. Then tests show up in Test Explorer and you can simply run them.

@alert Important
Remember to do both before pulling a PR or publishing new version
@end
