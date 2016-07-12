## Content

* [Building](#building)
* [Debugging](#debugging)
* [Running Tests](#running-tests)
* [Development](#development)
* [Chat room](#chat-room)
* [F#](#f-sharp)
* [NuGet](#nuget)

## Building

Required:

* [Visual Studio 2015 Update **3**](http://go.microsoft.com/fwlink/?LinkId=691129)
* [Latest NuGet Manager extension for Visual Studio](https://dist.nuget.org/visualstudio-2015-vsix/v3.5.0-beta/NuGet.Tools.vsix)
* [.NET Core SDK](https://go.microsoft.com/fwlink/?LinkID=809122)
* [.NET Core Tooling Preview 2 for Visual Studio 2015](https://go.microsoft.com/fwlink/?LinkId=817245)
* Internet connection and disk space to download all the required packages

If your build fails because some packages are not available, let say F#, then just disable these project and hope for nuget server to work later on ;)

## Debugging

![There are two debug profiles available in VS drop down](https://cloud.githubusercontent.com/assets/6011991/15627671/89f2405a-24eb-11e6-8bd1-c9d45613e0f6.png "Debug profiles")

## Running Tests

* To run "Classic" tests build the solution and run runClassicTests.cmd in the root directory or comment out the `netcoreapp1.0` part of all project.json files that belong to the testing projects.
* To run "Core" tests you just need to open Test Explorer in Visual Studio and rebuild the solution. Then tests show up in Test Explorer and you can simply run them.

**Remember to do both before pulling a PR or publishing new version**

## Development


### Branches
Please, use the `master` branch for developing. The `stable` branch should correspond the latest NuGet package of the library.

### New project files

.csproj and package.config files have been replaced with .xproj and project.json files. project.json automatically references all .cs files so you don’t have to update it with every new class/interface/enum added (number of git conflicts has just dropped).
It also has some side efects. For example if you create some subfolder in any of the folders that contain project.json file and put some .cs files there, then these files are going to be compiled as part of parent project by default. 

The other side effect is that xproj displays all files by default:
![xproj displays all files by default](/documentation/images/xprojDisplaysAllFilesByDefault.png?raw=true "xproj displays all files by default")

But if you want to include some files as resources, you have to do this in explicit way: 
```json
"buildOptions": {
    "embed": [ "Templates/*.txt", "Templates/*.R", "Templates/*.json" ]
}
```

 Project.json allows us to target multiple frameworks with one file and manage all dependencies in single place. Simplicity over complexity! 
 
```json
 "frameworks": {
    "net40": {
      "compilationOptions": {
        "define": [ "CLASSIC" ]
      },
      "frameworkAssemblies": {
        "System.Management": "4.0.0.0",
        "System.Xml": "4.0.0.0"
      }
    },
    "netstandard1.5": {
      "buildOptions": {
        "define": [ "CORE"]
      },
      "dependencies": {
        "System.Linq": "4.1.0",
        "System.Resources.ResourceManager": "4.0.1",
        "Microsoft.CSharp": "4.0.1",
        "Microsoft.Win32.Primitives": "4.0.1",
        "System.Console": "4.0.0",
        "System.Text.RegularExpressions": "4.1.0",
        "System.Threading": "4.0.11",
        "System.Reflection": "4.1.0",
        "System.Reflection.Primitives": "4.0.1",
        "System.Reflection.TypeExtensions": "4.1.0",
        "System.Threading.Thread": "4.0.0",
        "System.Diagnostics.Process": "4.1.0",
        "System.IO.FileSystem": "4.0.1",
        "System.Runtime.InteropServices.RuntimeInformation": "4.0.0",
        "System.Runtime.Serialization.Primitives": "4.1.1",
        "System.Diagnostics.Tools": "4.0.1",
        "System.Runtime.InteropServices": "4.1.0",
        "Microsoft.DotNet.InternalAbstractions": "1.0.0",
        "System.Reflection.Extensions": "4.0.1",
        "System.Diagnostics.Debug": "4.0.11",
        "System.Xml.XPath.XmlDocument": "4.0.1"
      }
    }
  }
```
 Project.json.lock tells the compiler exactly where to look for our dependencies. You can produce it with „dotnet restore”. Sometimes VS will do this for you, sometimes you will have to do this on your own.
 
### New dependencies

 There are at least 3 types of dependencies. Project, package and build. Sample:
```json
 "dependencies": {
    "BenchmarkDotNet": {
      "target": "project",
      "version": "1.0.0-*"
    }
  }
```
 When you want to add some dependency then you just add in in the right place in project.json. It depends on which platforms the library that you would like use  supports.
* If it supports all frameworks then you just need to move the dependencies to common dependencies (same level as frameworks, same thing applies to frameworkAssemblies).
```json
  "frameworks": {
    "net40": { },
    "netstandard1.5": { }
  },
  "dependencies": {
	"someCommonDependency": "it's version"
  }
```
* If there are few different packages/version then you need to specify both dependencies in explicit way:
```json
    "frameworks": {
	"net40": { 
		"dependencies": {
			"someCommonDependency": "exact version that supports net40"
	  }
    },
    "netstandard1.5": { 
		"dependencies": {
			"someCommonDependency": "exact version that supports netstandard1.5"
	  }
    }
  }
```
* If the desired package does not support all frameworks, then you add it as dependency to specific framework, but in code you use ugly #if #endif to exclude it for other compilation targets. We define #CLASSIC, #CORE. In other OSS projects you can meet more complex names like #NET40, #NET451, #DNXCORE50 or #NETCORE. 

```cs
#if CLASSIC
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace BenchmarkDotNet.Loggers
{
    internal class MsBuildConsoleLogger : Logger
    {
        private ILogger Logger { get; set; }

        public MsBuildConsoleLogger(ILogger logger)
        {
            Logger = logger;
        }

        public override void Initialize(IEventSource eventSource)
        {
            // By default, just show errors not warnings
            if (eventSource != null)
                eventSource.ErrorRaised += OnEventSourceErrorRaised;
        }

        private void OnEventSourceErrorRaised(object sender, BuildErrorEventArgs e) =>
            Logger.WriteLineError("// {0}({1},{2}): error {3}: {4}", e.File, e.LineNumber, e.ColumnNumber, e.Code, e.Message);
    }
}
#endif
```

* If it is not a package, but dll/exe file then you need to create wrap for it. You can do this for VS, but currently it will not work with dotnet cli, since it is going to produce wrong path. This is why I recommend to do this manually. You just need to create new folder in solution's root, then put project.json file there and **add the folder to global.json file**. Sample wrap file:
```json
{
  "version": "1.0.0-*",
  "frameworks": {
    "net40": {
      "bin": {
        "assembly": "../CLRMD/Dia2Lib.dll"
      }
    }
  }
}
```

## F# #

We have full F# support, all you have to do is to run `dotnet restore` to download the compilers etc.

## Chat room
[![Join the chat at https://gitter.im/PerfDotNet/BenchmarkDotNet](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/PerfDotNet/BenchmarkDotNet?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

## NuGet

If you want to check the develop version of the BenchmarkDotNet NuGet package, add the following line in the `<packageSources>` section of your `NuGet.congig`:
```xml
<add key="appveyor-bdn" value="https://ci.appveyor.com/nuget/benchmarkdotnet" />
```
Now you can install the package from the `appveyor-bdn` feed.