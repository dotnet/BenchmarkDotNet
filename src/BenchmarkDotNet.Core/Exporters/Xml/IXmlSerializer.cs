namespace BenchmarkDotNet.Exporters.Xml
{
    public interface IXmlSerializer : IFluentXmlSerializer
    {
        void Serialize(IXmlWriter writer, object source);
    }

    public interface IFluentXmlSerializer
    {
        IXmlSerializer WithRootName(string rootName);
        IXmlSerializer WithCollectionItemName(string collectionName, string itemName);
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
