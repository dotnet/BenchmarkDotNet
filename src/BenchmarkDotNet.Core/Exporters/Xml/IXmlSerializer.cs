using System;

namespace BenchmarkDotNet.Exporters.Xml
{
    public interface IXmlSerializer : IFluentXmlSerializer
    {
        void Serialize(IXmlWriter writer, object source);
    }

    public interface IFluentXmlSerializer
    {
        IXmlSerializer WithRootName(string rootName);
        IXmlSerializer WithCollectionItemName(Type type, string name);
        IXmlSerializer WithExcludedProperty(string propertyName);
    }

    public interface IXmlWriter
    {
        void WriteStartDocument();
        void WriteEndDocument();
        void WriteStartElement(string localName);
        void WriteEndElement();
        void WriteElementString(string localName, string value);
    }
}
