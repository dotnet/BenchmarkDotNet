using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Toolchains
{
    internal static class AppConfigGenerator
    {
        private static readonly HashSet<string> JobRuntimeSettings =
        [
            "useLegacyJit",
            "gcConcurrent",
            "gcServer",
            "GCCpuGroup",
            "gcAllowVeryLargeObjects",
            "GCHeapCount",
            "GCNoAffinitize",
            "GCHeapAffinitizeMask"
        ];

        internal static async ValueTask GenerateAsync(Job job, TextReader source, TextWriter destination, IResolver resolver, CancellationToken cancellationToken)
        {
            XDocument doc;
            XElement configurationElement;

            if (source == TextReader.Null)
            {
                configurationElement = new XElement("configuration");
                doc = new XDocument(new XDeclaration("1.0", "UTF-8", null), configurationElement);
            }
            else
            {
                doc = GetOrCreateDocument(source, out configurationElement);
            }

            var runtimeElement = GetOrCreateRuntimeElement(configurationElement);

            ClearStartupSettingsForCustomClr(configurationElement, job.Environment.Runtime);
            ClearAllRuntimeSettingsThatCanBeSetOnlyByJobConfiguration(runtimeElement);

            GenerateJitSettings(runtimeElement, job.Environment);
            GenerateGCSettings(runtimeElement, job.Environment.Gc, resolver);

#if NETSTANDARD2_0
            doc.Save(destination, SaveOptions.DisableFormatting);
#else
            await doc.SaveAsync(destination, SaveOptions.DisableFormatting, cancellationToken);
#endif
        }

        private static XDocument GetOrCreateDocument(TextReader source, out XElement configurationElement)
        {
            try
            {
                var doc = XDocument.Load(source);
                var configNode = doc.Element("configuration");
                if (configNode != null)
                {
                    configurationElement = configNode;
                    return doc;
                }

                // Document loaded but no <configuration> element
                configurationElement = new XElement("configuration");
                doc.Add(configurationElement);
                return doc;
            }
            catch (System.Xml.XmlException)
            {
                // Failed to load XML content, create a new document
                configurationElement = new XElement("configuration");
                return new XDocument(new XDeclaration("1.0", "UTF-8", null), configurationElement);
            }
        }

        private static XElement GetOrCreateRuntimeElement(XElement configurationElement)
        {
            return configurationElement.Element("runtime")
                   ?? AddAndReturn(configurationElement, new XElement("runtime"));
        }

        private static XElement AddAndReturn(XElement parent, XElement child)
        {
            parent.Add(child);
            return child;
        }

        private static void ClearAllRuntimeSettingsThatCanBeSetOnlyByJobConfiguration(XElement runtimeElement)
        {
            // Collect elements to remove first to avoid modifying collection during iteration.
            List<XElement>? toRemove = null;
            foreach (var child in runtimeElement.Elements())
            {
                if (JobRuntimeSettings.Contains(child.Name.LocalName))
                {
                    toRemove ??= new List<XElement>();
                    toRemove.Add(child);
                }
            }
            toRemove?.ForEach(e => e.Remove());
        }

        private static void ClearStartupSettingsForCustomClr(XElement configurationElement, Runtime? runtime)
        {
            if (!(runtime is ClrRuntime clrRuntime) || clrRuntime.Version.IsBlank())
                return;

            List<XElement>? toRemove = null;
            foreach (var child in configurationElement.Elements("startup"))
            {
                toRemove ??= new List<XElement>();
                toRemove.Add(child);
            }
            toRemove?.ForEach(e => e.Remove());
        }

        private static void GenerateJitSettings(XElement runtimeElement, EnvironmentMode environmentMode)
        {
            if (environmentMode.HasValue(EnvironmentMode.JitCharacteristic))
            {
                string useLegacyJit = environmentMode.Jit.ToConfig();
                runtimeElement.Add(new XElement("useLegacyJit", new XAttribute("enabled", useLegacyJit)));
            }
        }

        private static void GenerateGCSettings(XElement runtimeElement, GcMode gcMode, IResolver resolver)
        {
            Debug.Assert(JobRuntimeSettings.Contains("gcConcurrent"));
            Debug.Assert(JobRuntimeSettings.Contains("gcServer"));

            runtimeElement.Add(new XElement("gcConcurrent", new XAttribute("enabled", gcMode.ResolveValue(GcMode.ConcurrentCharacteristic, resolver).ToLowerCase())));
            runtimeElement.Add(new XElement("gcServer", new XAttribute("enabled", gcMode.ResolveValue(GcMode.ServerCharacteristic, resolver).ToLowerCase())));

            if (gcMode.HasValue(GcMode.CpuGroupsCharacteristic))
                runtimeElement.Add(new XElement("GCCpuGroup", new XAttribute("enabled", gcMode.ResolveValue(GcMode.CpuGroupsCharacteristic, resolver).ToLowerCase())));
            if (gcMode.HasValue(GcMode.AllowVeryLargeObjectsCharacteristic))
                runtimeElement.Add(new XElement("gcAllowVeryLargeObjects", new XAttribute("enabled", gcMode.ResolveValue(GcMode.AllowVeryLargeObjectsCharacteristic, resolver).ToLowerCase())));
            if (gcMode.HasValue(GcMode.NoAffinitizeCharacteristic))
                runtimeElement.Add(new XElement("GCNoAffinitize", new XAttribute("enabled", gcMode.ResolveValue(GcMode.NoAffinitizeCharacteristic, resolver).ToLowerCase())));
            if (gcMode.HasValue(GcMode.HeapAffinitizeMaskCharacteristic))
                runtimeElement.Add(new XElement("GCHeapAffinitizeMask", new XAttribute("enabled", gcMode.ResolveValue(GcMode.HeapAffinitizeMaskCharacteristic, resolver).ToString())));
            if (gcMode.HasValue(GcMode.HeapCountCharacteristic))
                runtimeElement.Add(new XElement("GCHeapCount", new XAttribute("enabled", gcMode.ResolveValue(GcMode.HeapCountCharacteristic, resolver).ToString())));
        }
    }
}
