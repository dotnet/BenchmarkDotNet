using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.DotNetCli;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Toolchains.MsBuildSdkExtras
{
    /// <summary>
    /// A tool chain which uses the MSBuild.Sdk.Extras SDK to allow for greater frameworks including Xamarin.
    /// </summary>
    [PublicAPI]
    public class MsBuildSdkExtrasToolchain : CsProjCoreToolchain
    {
        [PublicAPI] public static readonly IToolchain XamarinIOS10 = From(new NetCoreAppSettings("Xamarin.iOS10", null, "Xamarin iOS 1.0"));
        [PublicAPI] public static readonly IToolchain XamarinMac20 = From(new NetCoreAppSettings("Xamarin.Mac20", null, "Xamarin Mac 2.0"));
        [PublicAPI] public static readonly IToolchain XamarinTVOS10 = From(new NetCoreAppSettings("Xamarin.TVOS10", null, "Xamarin TV OS 1.0"));
        [PublicAPI] public static readonly IToolchain XamarinWatchOS10 = From(new NetCoreAppSettings("Xamarin.WatchOS10", null, "Xamarin Watch OS 1.0"));
        [PublicAPI] public static readonly IToolchain XamarinMonoAndroid10 = From(new NetCoreAppSettings("MonoAndroid10", null, "Xamarin Mono Android 1.0"));
        [PublicAPI] public static readonly IToolchain XamarinMonoAndroid23 = From(new NetCoreAppSettings("MonoAndroid23", null, "Xamarin Mono Android 2.3"));
        [PublicAPI] public static readonly IToolchain XamarinMonoAndroid403 = From(new NetCoreAppSettings("MonoAndroid403", null, "Xamarin Mono Android 4.0.3"));
        [PublicAPI] public static readonly IToolchain XamarinMonoAndroid41 = From(new NetCoreAppSettings("MonoAndroid41", null, "Xamarin Mono Android 4.1"));
        [PublicAPI] public static readonly IToolchain XamarinMonoAndroid42 = From(new NetCoreAppSettings("MonoAndroid42", null, "Xamarin Mono Android 4.2"));
        [PublicAPI] public static readonly IToolchain XamarinMonoAndroid43 = From(new NetCoreAppSettings("MonoAndroid43", null, "Xamarin Mono Android 4.3"));
        [PublicAPI] public static readonly IToolchain XamarinMonoAndroid44 = From(new NetCoreAppSettings("MonoAndroid44", null, "Xamarin Mono Android 4.4"));
        [PublicAPI] public static readonly IToolchain XamarinMonoAndroid4487 = From(new NetCoreAppSettings("MonoAndroid4487", null, "Xamarin Mono Android 4.4.87"));
        [PublicAPI] public static readonly IToolchain XamarinMonoAndroid50 = From(new NetCoreAppSettings("MonoAndroid50", null, "Xamarin Mono Android 5.0"));
        [PublicAPI] public static readonly IToolchain XamarinMonoAndroid51 = From(new NetCoreAppSettings("MonoAndroid51", null, "Xamarin Mono Android 5.1"));
        [PublicAPI] public static readonly IToolchain XamarinMonoAndroid60 = From(new NetCoreAppSettings("MonoAndroid60", null, "Xamarin Mono Android 6.0"));
        [PublicAPI] public static readonly IToolchain XamarinMonoAndroid70 = From(new NetCoreAppSettings("MonoAndroid70", null, "Xamarin Mono Android 7.0"));
        [PublicAPI] public static readonly IToolchain XamarinMonoAndroid71 = From(new NetCoreAppSettings("MonoAndroid71", null, "Xamarin Mono Android 7.1"));
        [PublicAPI] public static readonly IToolchain XamarinMonoAndroid80 = From(new NetCoreAppSettings("MonoAndroid80", null, "Xamarin Mono Android 8.0"));
        [PublicAPI] public static readonly IToolchain XamarinMonoAndroid81 = From(new NetCoreAppSettings("MonoAndroid81", null, "Xamarin Mono Android 8.1"));
        [PublicAPI] public static readonly IToolchain XamarinMonoAndroid90 = From(new NetCoreAppSettings("MonoAndroid90", null, "Xamarin Mono Android 9.0"));
        [PublicAPI] public static readonly IToolchain XamarinTouch10 = From(new NetCoreAppSettings("MonoTouch10", null, "Xamarin Mono Touch 1.0"));
        [PublicAPI] public static readonly IToolchain Tizen40 = From(new NetCoreAppSettings("Tizen40", null, "Tizen 4.0"));
        [PublicAPI] public static readonly IToolchain Tizen50 = From(new NetCoreAppSettings("Tizen50", null, "Tizen 5.0"));

        protected MsBuildSdkExtrasToolchain(string name, IGenerator generator, IBuilder builder, IExecutor executor, string customDotNetCliPath)
            : base(name, generator, builder, executor, customDotNetCliPath)
        {
        }

        [PublicAPI]
        public new static IToolchain From(NetCoreAppSettings settings)
            => new MsBuildSdkExtrasToolchain(settings.Name,
                new MsBuildSdkExtrasGenerator(settings.TargetFrameworkMoniker, settings.CustomDotNetCliPath, settings.PackagesPath, settings.RuntimeFrameworkVersion),
                new DotNetCliBuilder(settings.TargetFrameworkMoniker, settings.CustomDotNetCliPath, settings.Timeout),
                new DotNetCliExecutor(settings.CustomDotNetCliPath),
                settings.CustomDotNetCliPath);

    }
}
