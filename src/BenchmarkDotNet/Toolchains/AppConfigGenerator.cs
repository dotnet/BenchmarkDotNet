using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Toolchains
{
    internal static class AppConfigGenerator
    {
        private static readonly HashSet<string> JobRuntimeSettings = new HashSet<string>
        {
            "useLegacyJit",
            "gcConcurrent",
            "gcServer",
            "GCCpuGroup",
            "gcAllowVeryLargeObjects",
            "GCHeapCount",
            "GCNoAffinitize",
            "GCHeapAffinitizeMask"
        };

        internal static void Generate(Job job, TextReader source, TextWriter destination, IResolver resolver)
        {
            var xmlDocument = new XmlDocument();

            XmlNode configurationElement;

            if (source == TextReader.Null)
            {
                // Create a new configuration node.
                configurationElement = xmlDocument.CreateNode(XmlNodeType.Element, "configuration", string.Empty);
                xmlDocument.AppendChild(configurationElement);
            }
            else
            {
                // Try to get configuration node from specified TextReader.
                using var xmlReader = XmlReader.Create(source);
                configurationElement = GetOrCreateConfigurationElement(xmlDocument, xmlReader);
            }

            var runtimeElement = GetOrCreateRuntimeElement(xmlDocument, configurationElement);

            ClearStartupSettingsForCustomClr(configurationElement, job.Environment.Runtime);
            ClearAllRuntimeSettingsThatCanBeSetOnlyByJobConfiguration(runtimeElement);

            GenerateJitSettings(xmlDocument, runtimeElement, job.Environment);
            GenerateGCSettings(xmlDocument, runtimeElement, job.Environment.Gc, resolver);

            xmlDocument.Save(destination);
        }

        private static XmlNode GetOrCreateConfigurationElement(XmlDocument xmlDocument, XmlReader xmlReader)
        {
            try
            {
                xmlDocument.Load(xmlReader);
                var configurationNode = xmlDocument.SelectSingleNode("/configuration");
                if (configurationNode != null)
                    return configurationNode;
            }
            catch (XmlException)
            {
                // Failed to load XML content.
            }

            // If the XML is invalid or configuration node is not exists. Create a new configuration element
            return xmlDocument.AppendChild(xmlDocument.CreateNode(XmlNodeType.Element, "configuration", string.Empty))!;
        }

        private static XmlNode GetOrCreateRuntimeElement(XmlDocument xmlDocument, XmlNode configurationElement)
        {
            return configurationElement.SelectSingleNode("runtime")
                   ?? configurationElement.AppendChild(xmlDocument.CreateNode(XmlNodeType.Element, "runtime", string.Empty))!;
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

        private static void ClearStartupSettingsForCustomClr(XmlNode configurationElement, Runtime? runtime)
        {
            if (!(runtime is ClrRuntime clrRuntime) || clrRuntime.Version.IsBlank())
                return;

            foreach (XmlNode configurationChild in configurationElement.ChildNodes)
            {
                if (configurationChild.Name == "startup")
                {
                    configurationElement.RemoveChild(configurationChild);
                }
            }
        }

        private static void GenerateJitSettings(XmlDocument xmlDocument, XmlNode runtimeElement, EnvironmentMode environmentMode)
        {
            if (environmentMode.HasValue(EnvironmentMode.JitCharacteristic))
            {
                string useLegacyJit = environmentMode.Jit.ToConfig();
                CreateNodeWithAttribute(xmlDocument, runtimeElement, "useLegacyJit", "enabled", useLegacyJit);
            }
        }

        private static void GenerateGCSettings(XmlDocument xmlDocument, XmlNode runtimeElement, GcMode gcMode, IResolver resolver)
        {
            CreateNodeWithAttribute(xmlDocument, runtimeElement, "gcConcurrent", "enabled", gcMode.ResolveValue(GcMode.ConcurrentCharacteristic, resolver).ToLowerCase());
            CreateNodeWithAttribute(xmlDocument, runtimeElement, "gcServer", "enabled", gcMode.ResolveValue(GcMode.ServerCharacteristic, resolver).ToLowerCase());

            if (gcMode.HasValue(GcMode.CpuGroupsCharacteristic))
                CreateNodeWithAttribute(xmlDocument, runtimeElement, "GCCpuGroup", "enabled", gcMode.ResolveValue(GcMode.CpuGroupsCharacteristic, resolver).ToLowerCase());
            if (gcMode.HasValue(GcMode.AllowVeryLargeObjectsCharacteristic))
                CreateNodeWithAttribute(xmlDocument, runtimeElement, "gcAllowVeryLargeObjects", "enabled", gcMode.ResolveValue(GcMode.AllowVeryLargeObjectsCharacteristic, resolver).ToLowerCase());
            if (gcMode.HasValue(GcMode.NoAffinitizeCharacteristic))
                CreateNodeWithAttribute(xmlDocument, runtimeElement, "GCNoAffinitize", "enabled", gcMode.ResolveValue(GcMode.NoAffinitizeCharacteristic, resolver).ToLowerCase());
            if (gcMode.HasValue(GcMode.HeapAffinitizeMaskCharacteristic))
                CreateNodeWithAttribute(xmlDocument, runtimeElement, "GCHeapAffinitizeMask", "enabled", gcMode.ResolveValue(GcMode.HeapAffinitizeMaskCharacteristic, resolver).ToString());
            if (gcMode.HasValue(GcMode.HeapCountCharacteristic))
                CreateNodeWithAttribute(xmlDocument, runtimeElement, "GCHeapCount", "enabled", gcMode.ResolveValue(GcMode.HeapCountCharacteristic, resolver).ToString());
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
            if (node.Attributes == null)
                throw new NullReferenceException(nameof(node.Attributes));
            node.Attributes.SetNamedItem(attribute);

            parentNode.AppendChild(node);
        }
    }
}