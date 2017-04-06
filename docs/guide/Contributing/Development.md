# Development

When you want to add some dependency then you just use the "Add reference" option avaiable in Visual Studio. However, sometimes it might be not enough.

### If it supports all frameworks 

Just add it to the top level of csproj.

```xml
  <ItemGroup>
    <ProjectReference Include="..\..\src\BenchmarkDotNet\BenchmarkDotNet.csproj" />
  </ItemGroup>
```

### If the package/project is not available for all target frameworks

Specify a conditional dependency:

```xml
  <ItemGroup Condition=" '$(TargetFramework)' == 'net46' ">
    <ProjectReference Include="..\..\src\BenchmarkDotNet.Diagnostics.Windows\BenchmarkDotNet.Diagnostics.Windows.csproj" />
  </ItemGroup>
```

Once you add it as dependency to specific framework, you need to use ugly #if #endif to exclude it for other compilation targets. 
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