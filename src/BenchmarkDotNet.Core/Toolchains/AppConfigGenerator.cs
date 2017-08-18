using BenchmarkDotNet.Jobs;
using System.IO;
using System.Xml;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using System.Collections.Generic;
using System.Diagnostics;

namespace BenchmarkDotNet.Toolchains
{
    internal static class AppConfigGenerator
    {
        private static readonly HashSet<string> JobRuntimeSettings = new HashSet<string>()
        {
            "useLegacyJit",
            "gcConcurrent",
            "gcServer",
            "GCCpuGroup",
            "gcAllowVeryLargeObjects"
        };

        internal static void Generate(Job job, TextReader source, TextWriter destination, IResolver resolver)
        {
            var xmlReader = XmlReader.Create(source);
            var xmlDocument = new XmlDocument();

            var configurationElement = GetOrCreateConfigurationElement(xmlDocument, xmlReader);
            var runtimeElement = GetOrCreateRuntimeElement(xmlDocument, configurationElement);

            ClearAllRuntimeSettingsThatCanBeSetOnlyByJobConfiguration(runtimeElement);

            GenerateJitSettings(xmlDocument, runtimeElement, job.Env);
            GenerateGCSettings(xmlDocument, runtimeElement, job.Env.Gc, resolver);

            xmlDocument.Save(destination);
        }

        private static XmlNode GetOrCreateConfigurationElement(XmlDocument xmlDocument, XmlReader xmlReader)
        {
            try
            {
                xmlDocument.Load(xmlReader);

                return xmlDocument.SelectSingleNode("/configuration");
            }
            catch // empty document
            {
                return xmlDocument.AppendChild(xmlDocument.CreateNode(XmlNodeType.Element, "configuration", string.Empty));
            }
        }

        private static XmlNode GetOrCreateRuntimeElement(XmlDocument xmlDocument, XmlNode configurationElement)
        {
            return configurationElement.SelectSingleNode("runtime")
                   ?? configurationElement.AppendChild(xmlDocument.CreateNode(XmlNodeType.Element, "runtime", string.Empty));
        }

        private static void ClearAllRuntimeSettingsThatCanBeSetOnlyByJobConfiguration(XmlNode runtimeElement)
        {
            foreach (XmlNode runtimeSetting in runtimeElement.ChildNodes)
            {
                if (JobRuntimeSettings.Contains(runtimeSetting.Name))
                {
                    runtimeElement.RemoveChild(runtimeSetting);
                }
            }
        }

        private static void GenerateJitSettings(XmlDocument xmlDocument, XmlNode runtimeElement, EnvMode envMode)
        {
            if (envMode.HasValue(EnvMode.JitCharacteristic))
            {
                string useLegacyJit = envMode.Jit == Jit.RyuJit ? "0" : "1";
                CreateNodeWithAttribute(xmlDocument, runtimeElement, "useLegacyJit", "enabled", useLegacyJit);
            }
        }

        private static void GenerateGCSettings(XmlDocument xmlDocument, XmlNode runtimeElement, GcMode gcMode, IResolver resolver)
        {
            if (!gcMode.HasChanges)
                return;

            CreateNodeWithAttribute(xmlDocument, runtimeElement, "gcConcurrent", "enabled", gcMode.ResolveValue(GcMode.ConcurrentCharacteristic, resolver).ToLowerCase());
            CreateNodeWithAttribute(xmlDocument, runtimeElement, "gcServer", "enabled", gcMode.ResolveValue(GcMode.ServerCharacteristic, resolver).ToLowerCase());
            CreateNodeWithAttribute(xmlDocument, runtimeElement, "GCCpuGroup", "enabled", gcMode.ResolveValue(GcMode.CpuGroupsCharacteristic, resolver).ToLowerCase());
            CreateNodeWithAttribute(xmlDocument, runtimeElement, "gcAllowVeryLargeObjects", "enabled", gcMode.ResolveValue(GcMode.AllowVeryLargeObjectsCharacteristic, resolver).ToLowerCase());
        }

        private static void CreateNodeWithAttribute(
            XmlDocument document,
            XmlNode parentNode,
            string nodeName,
            string attributeName,
            string attributeValue)
        {
            Debug.Assert(JobRuntimeSettings.Contains(nodeName), "Please add the new setting to the JobRuntimeSettings list");

            var node = document.CreateNode(XmlNodeType.Element, nodeName, string.Empty);
            var attribute = document.CreateAttribute(attributeName);
            attribute.Value = attributeValue;
            node.Attributes.SetNamedItem(attribute);

            parentNode.AppendChild(node);
        }
    }
}