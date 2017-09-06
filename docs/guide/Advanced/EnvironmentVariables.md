# Environment Variables

You can configure custom environment variables for the process that is running your benchmarks. One reason for doing this might be checking out how different [runtime knobs](https://github.com/dotnet/coreclr/blob/master/Documentation/project-docs/clr-configuration-knobs.md) affect the performance of Core CLR.

**Warning:** This feature does not work for .NET Core 1.1 (`ProcessStartInfo.EnvironmentVariables` is available for .NET Core 2.0+)

## Sample configuration

```cs
public class ConfigWithCustomEnvVars : ManualConfig
{
    public ConfigWithCustomEnvVars()
    {
        Add(Job.Core.WithId("Inlining enabled"));
        Add(Job.Core.With(
            new[] { new EnvironmentVariable("COMPlus_JitNoInline", "1") })
            .WithId("Inlining disabled"));
    }
}
```