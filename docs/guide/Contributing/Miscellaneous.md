#Miscellaneous topics
 
## F# #

We have full F# support, all you have to do is to run `dotnet restore` to download the compilers etc.

## Chat room
[![Join the chat at https://gitter.im/PerfDotNet/BenchmarkDotNet](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/PerfDotNet/BenchmarkDotNet?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

## How can I help?

[Here is a list of up-for-grabs issues](https://github.com/PerfDotNet/BenchmarkDotNet/issues?q=is%3Aissue+is%3Aopen+label%3Aup-for-grabs)

## Nightly NuGet feed

If you want to check the develop version of the BenchmarkDotNet NuGet package, add the following line in the `<packageSources>` section of your `NuGet.config`:
```xml
<add key="appveyor-bdn" value="https://ci.appveyor.com/nuget/benchmarkdotnet" />
```
Now you can install the package from the `appveyor-bdn` feed.