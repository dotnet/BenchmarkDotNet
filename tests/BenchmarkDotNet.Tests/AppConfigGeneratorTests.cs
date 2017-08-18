using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains;
using System;
using System.IO;
using System.Text;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Running;
using Xunit;

namespace BenchmarkDotNet.Tests
{
    public class AppConfigGeneratorTests
    {
        private static readonly IResolver Resolver = BenchmarkRunnerCore.DefaultResolver;

        [Fact]
        public void GeneratesMinimalRequiredAppConfigForEmptySource()
        {
            using (var destination = new Utf8StringWriter())
            {
                const string expectedMinimal =
                    "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                    "<configuration>" +
                    "<runtime/>" +
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
                const string expectedMinimal =
                    "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                    "<configuration>" +
                    "<runtime/>" +
                    "</configuration>";

                AppConfigGenerator.Generate(Job.Default, source, destination, Resolver);

                AssertAreEqualIgnoringWhitespacesAndCase(expectedMinimal, destination.ToString());
            }
        }

        [Fact]
        public void RewritesCutomSettings()
        {
            const string customSettings =
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<!--" +
                "commentsAreSupported" +
                "-->" +
                "<configuration>" +
                "<someConfig>withItsValue</someConfig>" +
                "<runtime/>" +
                "</configuration>";

            using (var source = new StringReader(customSettings))
            using (var destination = new Utf8StringWriter())
            {
                AppConfigGenerator.Generate(Job.Default, source, destination, Resolver);

                AssertAreEqualIgnoringWhitespacesAndCase(customSettings, destination.ToString());
            }
        }

        [Fact]
        public void RewritesCutomRuntimeSettings()
        {
            const string customSettings =
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<!--" +
                "commentsAreSupported" +
                "-->" +
                "<configuration>" +
                "<someConfig>withItsValue</someConfig>" +
                "<runtime><AppContextSwitchOverrides value=\"Switch.System.IO.UseLegacyPathHandling=false\"/></runtime>" +
                "</configuration>";

            using (var source = new StringReader(customSettings))
            using (var destination = new Utf8StringWriter())
            {
                AppConfigGenerator.Generate(Job.Default, source, destination, Resolver);

                AssertAreEqualIgnoringWhitespacesAndCase(customSettings, destination.ToString());
            }
        }

        [Theory]
        [InlineData(Jit.LegacyJit, "<runtime><useLegacyJit enabled=\"1\" /></runtime>")]
        [InlineData(Jit.RyuJit, "<runtime><useLegacyJit enabled=\"0\" /></runtime>")]
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
                expectedRuntimeNode +
                "</configuration>" + Environment.NewLine;

            using (var source = new StringReader(customSettings))
            using (var destination = new Utf8StringWriter())
            {
                AppConfigGenerator.Generate(new Job { Env = { Jit = jit } }.Freeze(), source, destination, Resolver);

                AssertAreEqualIgnoringWhitespacesAndCase(customSettingsAndJit, destination.ToString());
            }
        }

        [Fact]
        public void RewritesCutomAssemblyBindingRedirects()
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

            const string settingsWithBindingsAndJit =
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
    }

    internal class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
    }
}