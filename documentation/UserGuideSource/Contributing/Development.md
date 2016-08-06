# Development

## Branches
Please, use the `develop` branch for developing. The `master` branch should correspond the latest NuGet package of the library.

## New project files

.csproj and package.config files have been replaced with .xproj and project.json files. project.json automatically references all .cs files so you 
don’t have to update it with every new class/interface/enum added (number of git conflicts has just dropped). It also has some side efects. 
For example if you create some subfolder in any of the folders that contain project.json file and put some .cs files there, then these files are 
going to be compiled as part of parent project by default. 

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
 
## New dependencies

 There are at least 3 types of dependencies. Project, package and build. Sample:

 ```json
 "dependencies": {
    "BenchmarkDotNet": {
      "target": "project",
      "version": "1.0.0-*"
    }
  }
```

When you want to add some dependency then you just add in in the right place in project.json. It depends on which platforms the library that you would like use supports.

### If it supports all frameworks 

Just to move the dependencies to common dependencies (same level as frameworks, same thing applies to frameworkAssemblies).

```json
  "frameworks": {
    "net40": { },
    "netstandard1.5": { }
  },
  "dependencies": {
	"someCommonDependency": "it's version"
  }
```

### If there are few different packages/version

Specify both dependencies in explicit way:

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

###If the desired package does not support all frameworks

Add it as dependency to specific framework, but in code you use ugly #if #endif to exclude it for other compilation targets. 
We define #CLASSIC, #CORE. In other OSS projects you can meet more complex names like #NET40, #NET451, #DNXCORE50 or #NETCORE. 

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

### If it is not a package, but dll/exe file

Create a wrap for it. You can do this for VS, but currently it will not work with dotnet cli, since it is going to produce wrong path. This is why I recommend to do this manually. You just need to create new folder in solution's root, then put project.json file there and **add the folder to global.json file**. Sample wrap file:

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
