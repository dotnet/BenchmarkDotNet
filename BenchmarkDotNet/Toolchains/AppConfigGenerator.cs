using BenchmarkDotNet.Jobs;
using System.IO;
using System.Xml;
using BenchmarkDotNet.Helpers;

namespace BenchmarkDotNet.Toolchains
{
    internal static class AppConfigGenerator
    {
        internal static void Generate(IJob job, TextReader source, TextWriter destination)
        {
            var xmlReader = XmlReader.Create(source);
            var xmlDocument = new XmlDocument();

            var configurationElement = GetOrCreateConfigurationElement(xmlDocument, xmlReader);
            var runtimeElement = GetOrCreateRuntimeElement(xmlDocument, configurationElement);

            ClearAllCustomRuntimeSettingsExceptRedirects(runtimeElement);

            GenerateJitSettings(xmlDocument, runtimeElement, job.Jit);
            GenerateGCSettings(xmlDocument, runtimeElement, job.GarbageCollection);

            xmlDocument.Save(destination);
        }

        private static XmlNode GetOrCreateConfigurationElement(XmlDocument xmlDocument, XmlReader xmlReader)
        {
            try
            {
                xmlDocument.Load(xmlReader);

                return xmlDocument.SelectSingleNode("/configuration");
            }
            catch (XmlException) // empty document
            {
                return xmlDocument.AppendChild(xmlDocument.CreateNode(XmlNodeType.Element, "configuration", string.Empty));
            }
        }

        private static XmlNode GetOrCreateRuntimeElement(XmlDocument xmlDocument, XmlNode configurationElement)
        {
            return configurationElement.SelectSingleNode("runtime")
                ?? configurationElement.AppendChild(xmlDocument.CreateNode(XmlNodeType.Element, "runtime", string.Empty));
        }

        private static void ClearAllCustomRuntimeSettingsExceptRedirects(XmlNode runtimeElement)
        {
            foreach (XmlNode runtimeSetting in runtimeElement.ChildNodes)
            {
                if (runtimeSetting.Name != "assemblyBinding")
                {
                    runtimeElement.RemoveChild(runtimeSetting);
                }
            }
        }

        private static void GenerateJitSettings(XmlDocument xmlDocument, XmlNode runtimeElement, Jit jit)
        {
            if (jit == Jit.Host)
            {
                return;
            }

            CreateNodeWithAttribute(xmlDocument, runtimeElement, "useLegacyJit", "enabled",
                jit == Jit.RyuJit || (jit == Jit.Host && HostEnvironmentInfo.GetCurrent().HasRyuJit)
                    ? "0"
                    : "1");
        }

        private static void GenerateGCSettings(XmlDocument xmlDocument, XmlNode runtimeElement, GarbageCollection garbageCollection)
        {
            if (garbageCollection == null || garbageCollection == GarbageCollection.Default)
            {
                return;
            }

            CreateNodeWithAttribute(xmlDocument, runtimeElement, "gcConcurrent", "enabled", garbageCollection.Concurrent.ToString().ToLower());
            CreateNodeWithAttribute(xmlDocument, runtimeElement, "gcServer", "enabled", garbageCollection.Server.ToString().ToLower());
            CreateNodeWithAttribute(xmlDocument, runtimeElement, "GCCpuGroup", "enabled", garbageCollection.CpuGroups.ToString().ToLower());
            CreateNodeWithAttribute(xmlDocument, runtimeElement, "gcAllowVeryLargeObjects", "enabled", garbageCollection.AllowVeryLargeObjects.ToString().ToLower());
        }

        private static void CreateNodeWithAttribute(
            XmlDocument document,
            XmlNode parentNode,
            string nodeName,
            string attributeName,
            string attributeValue)
        {
            var node = document.CreateNode(XmlNodeType.Element, nodeName, string.Empty);
            var attribute = document.CreateAttribute(attributeName);
            attribute.Value = attributeValue;
            node.Attributes.SetNamedItem(attribute);

            parentNode.AppendChild(node);
        }
    }
}
