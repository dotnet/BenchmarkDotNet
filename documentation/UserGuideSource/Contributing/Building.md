# Building

For building the BenchmarkDotNet source-code, the following elements are required:

* [Visual Studio 2015 Update **3**](http://go.microsoft.com/fwlink/?LinkId=691129)
* [Latest NuGet Manager extension for Visual Studio](https://dist.nuget.org/visualstudio-2015-vsix/v3.5.0-beta/NuGet.Tools.vsix)
* [.NET Core SDK](https://go.microsoft.com/fwlink/?LinkID=809122)
* [.NET Core Tooling Preview 2 for Visual Studio 2015](https://go.microsoft.com/fwlink/?LinkId=817245)
* Internet connection and disk space to download all the required packages

If your build fails because some packages are not available, let say F#, then just disable these project and hope for nuget server to work later on ;)
