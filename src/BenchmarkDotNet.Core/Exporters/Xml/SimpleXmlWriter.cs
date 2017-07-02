using System;
using System.IO;
using System.Xml;

namespace BenchmarkDotNet.Exporters.Xml
{
    internal class SimpleXmlWriter : IXmlWriter, IDisposable
    {
        private XmlWriter writer;

        public SimpleXmlWriter(TextWriter writer, bool indent)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            this.writer = XmlWriter.Create(writer, new XmlWriterSettings() { Indent = indent });
        }

        public void WriteElementString(string localName, string value)
        {
            writer.WriteElementString(localName, value);
        }

        public void WriteEndDocument()
        {
            writer.WriteEndDocument();
        }

        public void WriteEndElement()
        {
            writer.WriteEndElement();
        }

        public void WriteStartDocument()
        {
            writer.WriteStartDocument();
        }

        public void WriteStartElement(string localName)
        {
            writer.WriteStartElement(localName);
        }

        // Dispose
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    writer.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
