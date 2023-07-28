using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains;
using System;
using System.IO;
using System.Text;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Running;
using Xunit;
using BenchmarkDotNet.Tests.XUnit;
using System.Runtime;

namespace BenchmarkDotNet.Tests
{
    public class AppConfigGeneratorTests
    {
        private static readonly IResolver Resolver = BenchmarkRunnerClean.DefaultResolver;

        [Fact]
        public void GeneratesMinimalRequiredAppConfigForEmptySource()
        {
            using (var destination = new Utf8StringWriter())
            {
                string expectedMinimal =
                    "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                    "<configuration>" +
                    $"<runtime>{GcSettings}</runtime>" +
                    "</configuration>";

                AppConfigGenerator.Generate(Job.Default, TextReader.Null, destination, Resolver);

                AssertAreEqualIgnoringWhitespacesAndCase(expectedMinimal, destination.ToString());
            }
        }

        [Fact]
        public void GeneratesMinimalRequiredAppConfigForAlmostEmptySource()
        {
            using (var source = new StringReader("<?xml version=\"1.0\" encoding=\"UTF-8\"?>"))
            using (var destination = new Utf8StringWriter())
            {
                string expectedMinimal =
                    "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                    "<configuration>" +
                    $"<runtime>{GcSettings}</runtime>" +
                    "</configuration>";

                AppConfigGenerator.Generate(Job.Default, source, destination, Resolver);

                AssertAreEqualIgnoringWhitespacesAndCase(expectedMinimal, destination.ToString());
            }
        }

        [Fact]
        public void RewritesCustomSettings()
        {
            string customSettingsWithoutRuntimeNode =
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<!--" +
                "commentsAreSupported" +
                "-->" +
                "<configuration>" +
                "<someConfig>withItsValue</someConfig>" +
                "</configuration>";

            string customSettingsWithRuntimeNode =
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<!--" +
                "commentsAreSupported" +
                "-->" +
                "<configuration>" +
                "<someConfig>withItsValue</someConfig>" +
                $"<runtime>{GcSettings}</runtime>" +
                "</configuration>";

            using (var source = new StringReader(customSettingsWithoutRuntimeNode))
            using (var destination = new Utf8StringWriter())
            {
                AppConfigGenerator.Generate(Job.Default, source, destination, Resolver);

                AssertAreEqualIgnoringWhitespacesAndCase(customSettingsWithRuntimeNode, destination.ToString());
            }
        }

        [Fact]
        public void RewritesCustomRuntimeSettings()
        {
            string customSettingsBefore =
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<!--" +
                "commentsAreSupported" +
                "-->" +
                "<configuration>" +
                "<someConfig>withItsValue</someConfig>" +
                $"<runtime><AppContextSwitchOverrides value=\"Switch.System.IO.UseLegacyPathHandling=false\"/></runtime>" +
                "</configuration>";

            string customSettingsAfter =
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<!--" +
                "commentsAreSupported" +
                "-->" +
                "<configuration>" +
                "<someConfig>withItsValue</someConfig>" +
                $"<runtime><AppContextSwitchOverrides value=\"Switch.System.IO.UseLegacyPathHandling=false\"/>{GcSettings}</runtime>" +
                "</configuration>";

            using (var source = new StringReader(customSettingsBefore))
            using (var destination = new Utf8StringWriter())
            {
                AppConfigGenerator.Generate(Job.Default, source, destination, Resolver);

                AssertAreEqualIgnoringWhitespacesAndCase(customSettingsAfter, destination.ToString());
            }
        }

        [Theory]
        [InlineData(Jit.LegacyJit, "<useLegacyJit enabled=\"1\" />")]
        [InlineData(Jit.RyuJit, "<useLegacyJit enabled=\"0\" />")]
        public void GeneratesRightJitSettings(Jit jit, string expectedRuntimeNode)
        {
            const string customSettings =
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<configuration>" +
                "<someConfig>withItsValue</someConfig>" +
                "</configuration>";

            string customSettingsAndJit =
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<configuration>" +
                "<someConfig>withItsValue</someConfig>" +
                $"<runtime>{expectedRuntimeNode}{GcSettings}</runtime>" +
                "</configuration>" + Environment.NewLine;

            using (var source = new StringReader(customSettings))
            using (var destination = new Utf8StringWriter())
            {
                AppConfigGenerator.Generate(new Job { Environment = { Jit = jit } }.Freeze(), source, destination, Resolver);

                AssertAreEqualIgnoringWhitespacesAndCase(customSettingsAndJit, destination.ToString());
            }
        }

        [FactEnvSpecific("Full Framework is supported only on Windows", EnvRequirement.WindowsOnly)]
        public void RemovesStartupSettingsForPrivateBuildsOfClr()
        {
            const string input =
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<configuration>" +
                "<startup><supportedRuntime version=\"v4.0\" sku=\".NETFramework,Version=v4.6.2\" /></startup>" +
                "</configuration>";

            string withoutStartup =
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<configuration>" +
                $"<runtime>{GcSettings}</runtime>" +
                "</configuration>" + Environment.NewLine;

            using (var source = new StringReader(input))
            using (var destination = new Utf8StringWriter())
            {
                AppConfigGenerator.Generate(new Job { Environment = { Runtime = ClrRuntime.CreateForLocalFullNetFrameworkBuild(version: "4.0")} }.Freeze(), source, destination, Resolver);

                AssertAreEqualIgnoringWhitespacesAndCase(withoutStartup, destination.ToString());
            }
        }

        [Fact]
        public void LeavsStartupSettingsIntactForNonPrivateBuildsOfClr()
        {
            const string input =
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<configuration>" +
                "<startup><supportedRuntime version=\"v4.0\" sku=\".NETFramework,Version=v4.6.2\" /></startup>" +
                "</configuration>";

            string withoutStartup =
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<configuration>" +
                "<startup><supportedRuntime version=\"v4.0\" sku=\".NETFramework,Version=v4.6.2\" /></startup>" +
                $"<runtime>{GcSettings}</runtime>" +
                "</configuration>" + Environment.NewLine;

            using (var source = new StringReader(input))
            using (var destination = new Utf8StringWriter())
            {
                AppConfigGenerator.Generate(new Job { Environment = { Runtime = ClrRuntime.Net462 } }.Freeze(), source, destination, Resolver);

                AssertAreEqualIgnoringWhitespacesAndCase(withoutStartup, destination.ToString());
            }
        }

        [Fact]
        public void RewritesCustomAssemblyBindingRedirects()
        {
            const string settingsWithBindings =
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<configuration>" +
                "<runtime>" +
                "<assemblyBinding xmlns=\"urn:schemas-microsoft-com:asm.v1\">" +
                "<dependentAssembly>" +
                "<assemblyIdentity name=\"System.Runtime\" publicKeyToken=\"b03f5f7f11d50a3a\" culture=\"neutral\" />" +
                "<bindingRedirect oldVersion=\"0.0.0.0-4.0.20.0\" newVersion=\"4.0.20.0\" />" +
                "</dependentAssembly>" +
                "</assemblyBinding>" +
                "</runtime>" +
                "</configuration>";

            string settingsWithBindingsAndJit =
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<configuration>" +
                "<runtime>" +
                "<assemblyBinding xmlns=\"urn:schemas-microsoft-com:asm.v1\">" +
                "<dependentAssembly>" +
                "<assemblyIdentity name=\"System.Runtime\" publicKeyToken=\"b03f5f7f11d50a3a\" culture=\"neutral\" />" +
                "<bindingRedirect oldVersion=\"0.0.0.0-4.0.20.0\" newVersion=\"4.0.20.0\" />" +
                "</dependentAssembly>" +
                "</assemblyBinding>" +
                "<useLegacyJit enabled =\"0\" />" +
                GcSettings +
                "</runtime>" +
                "</configuration>";

            using (var source = new StringReader(settingsWithBindings))
            using (var destination = new Utf8StringWriter())
            {
                AppConfigGenerator.Generate(Job.RyuJitX64, source, destination, Resolver);

                AssertAreEqualIgnoringWhitespacesAndCase(settingsWithBindingsAndJit, destination.ToString());
            }
        }

        private static void AssertAreEqualIgnoringWhitespacesAndCase(string expectedXml, string actualXml)
        {
            string expected = RemoveWhiteSpaces(expectedXml);
            string actual = RemoveWhiteSpaces(actualXml);

            Assert.Equal(expected, actual, StringComparer.OrdinalIgnoreCase);
        }

        private static string RemoveWhiteSpaces(string input)
        {
            var buffer = new StringBuilder(input.Length);
            foreach (char character in input)
                switch (character)
                {
                    case '\r':
                    case '\n':
                    case '\t':
                    case ' ':
                        continue;
                    default:
                        buffer.Append(character);
                        break;
                }
            return buffer.ToString();
        }

        private static readonly string GcSettings = $"<gcConcurrentenabled=\"{(GCSettings.LatencyMode != GCLatencyMode.Batch).ToLowerCase()}\"/><gcServerenabled=\"{GCSettings.IsServerGC.ToLowerCase()}\"/>";
    }

    internal class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
    }
}