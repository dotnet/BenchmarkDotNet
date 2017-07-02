namespace BenchmarkDotNet.Exporters.Xml
{
    internal interface IXmlSerializer
    {
        void Serialize(IXmlWriter writer, object source);
        IXmlSerializer WithRootName(string rootName);
        IXmlSerializer WithCollectionItemName(string collectionName, string itemName);
        IXmlSerializer WithExcludedProperty(string propertyName);
    }

    internal interface IXmlWriter
    {
        void WriteStartDocument();
        void WriteEndDocument();
        void WriteStartElement(string localName);
        void WriteEndElement();
        void WriteElementString(string localName, string value);
    }
}
