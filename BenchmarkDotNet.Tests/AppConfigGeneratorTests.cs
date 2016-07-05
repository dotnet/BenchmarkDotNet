using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains;
using System;
using System.IO;
using System.Text;
using Xunit;

namespace BenchmarkDotNet.Tests
{
    public class AppConfigGeneratorTests
    {
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

                AppConfigGenerator.Generate(Job.Default, TextReader.Null, destination);

                Assert.True(AreEqualIgnoringWhitespacesAndCase(expectedMinimal, destination.ToString()));
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

                AppConfigGenerator.Generate(Job.Default, source, destination);

                Assert.True(AreEqualIgnoringWhitespacesAndCase(expectedMinimal, destination.ToString()));
            }
        }

        [Fact]
        public void RewritesCutomSettings()
        {
            string customSettings =
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
                AppConfigGenerator.Generate(Job.Default, source, destination);

                Assert.True(AreEqualIgnoringWhitespacesAndCase(customSettings, destination.ToString()));
            }
        }

        [Theory]
        [InlineData(Jit.Host, "<runtime/>")]
        [InlineData(Jit.LegacyJit, "<runtime><useLegacyJit enabled=\"1\" /></runtime>")]
        [InlineData(Jit.RyuJit, "<runtime><useLegacyJit enabled=\"0\" /></runtime>")]
        public void GeneratesRightJitSettings(Jit jit, string expectedRuntimeNode)
        {
            string customSettings =
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
                AppConfigGenerator.Generate(Job.Default.With(jit), source, destination);

                Assert.True(AreEqualIgnoringWhitespacesAndCase(customSettingsAndJit, destination.ToString()));
            }
        }

        [Fact]
        public void RewritesCutomAssemblyBindingRedirects()
        {
            string settingsWithBindings =
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
                    "</runtime>" +
                "</configuration>";

            using (var source = new StringReader(settingsWithBindings))
            using (var destination = new Utf8StringWriter())
            {
                AppConfigGenerator.Generate(Job.RyuJitX64, source, destination);

                Assert.True(AreEqualIgnoringWhitespacesAndCase(settingsWithBindingsAndJit, destination.ToString()));
            }
        }

        bool AreEqualIgnoringWhitespacesAndCase(string expectedXml, string actualXml)
        {
            var expectedNoWhiteSpaces = RemoveWhiteSpaces(expectedXml);
            var actualNoWhiteSpaces = RemoveWhiteSpaces(actualXml);

            return expectedNoWhiteSpaces.Equals(actualNoWhiteSpaces, StringComparison.OrdinalIgnoreCase);
        }

        string RemoveWhiteSpaces(string input)
        {
            StringBuilder buffer = new StringBuilder(input.Length);
            for (int i = 0; i < input.Length; i++)
            {
                char character = input[i];
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
            }
            return buffer.ToString();
        }
    }

    internal class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
    }
}
