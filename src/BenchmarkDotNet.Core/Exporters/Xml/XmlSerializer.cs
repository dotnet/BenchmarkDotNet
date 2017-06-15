using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Xml;

namespace BenchmarkDotNet.Exporters.Xml
{
    public class XmlSerializer
    {
        private readonly Type type;
        private readonly Dictionary<Type, string> itemNames = new Dictionary<Type, string>();
        private readonly HashSet<string> excludedProperties = new HashSet<string>();
        private XmlWriter writer;
        private string rootName;

        public XmlSerializer(Type type)
        {
            if (type == null)
                throw new NullReferenceException();

            this.type = type;
            rootName = type.Name;
        }

        public XmlSerializer WithRootName(string rootName)
        {
            if (string.IsNullOrWhiteSpace(rootName))
                throw new ArgumentException(nameof(rootName));

            this.rootName = rootName;
            return this;
        }

        public XmlSerializer WithCollectionItemName(Type type, string name)
        {
            if (type == null)
                throw new NullReferenceException(nameof(type));
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException(nameof(name));

            itemNames.Add(type, name);
            return this;
        }

        public XmlSerializer WithExcludedProperty(string propertyName)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
                throw new ArgumentException(nameof(propertyName));

            excludedProperties.Add(propertyName);
            return this;
        }

        public void Serialize(XmlWriter writer, object source)
        {
            if (writer == null)
                throw new NullReferenceException(nameof(writer));
            if (source == null)
                throw new NullReferenceException(nameof(source));

            this.writer = writer;

            Write(source);
        }

        private void Write(object source)
        {
            writer.WriteStartDocument();
            WriteRoot(source, rootName);
            writer.WriteEndDocument();
        }

        private void WriteRoot(object source, string elementName)
        {
            writer.WriteStartElement(elementName);

            foreach (var property in type.GetProperties())
            {
                WriteProperty(source, property);
            }

            writer.WriteEndElement();
        }

        private void WriteProperty(object source, PropertyInfo property)
        {
            if (source == null || excludedProperties.Contains(property.Name))
                return;
            
            if (IsSimple(property.PropertyType.GetTypeInfo()))
            {
                WriteSimpleProperty(source, property);
            }
            else if (IsCollection(property))
            {
                WriteCollectionProperty(source, property);
            }
            else
            {
                WriteComplexProperty(property.GetValue(source), property);
            }
        }

        private void WriteSimpleProperty(object source, PropertyInfo property)
        {
            string value = string.Format(
                CultureInfo.InvariantCulture, "{0}", property.GetValue(source));

            if (!string.IsNullOrEmpty(value))
            {
                writer.WriteElementString(property.Name, value);
            }
        }

        private void WriteComplexProperty(object source, PropertyInfo propertyInfo)
        {
            writer.WriteStartElement(propertyInfo.Name);

            foreach (var property in propertyInfo.PropertyType.GetProperties())
            {
                WriteProperty(source, property);
            }

            writer.WriteEndElement();
        }
        
        private void WriteCollectionProperty(object source, PropertyInfo property)
        {
            IEnumerable collection = (IEnumerable)property.GetValue(source);

            if (!IsCollectionWriteable(collection))
                return;

            writer.WriteStartElement(property.Name);

            string itemName = null;

            foreach (var item in collection)
            {
                if (itemName == null)
                {
                    itemName = itemNames[item.GetType()];
                }

                if (IsSimple(item.GetType().GetTypeInfo()))
                {
                    writer.WriteElementString(itemName,
                        string.Format(CultureInfo.InvariantCulture, "{0}", item));
                }
                else
                {
                    WriteComplexItem(item, itemName);
                }
            }

            writer.WriteEndElement();
        }

        private void WriteComplexItem(object item, string itemName)
        {
            writer.WriteStartElement(itemName);

            foreach (var property in item.GetType().GetProperties())
            {
                WriteProperty(item, property);
            }

            writer.WriteEndElement();
        }

        private static bool IsSimple(TypeInfo type)
        {
            return type.IsPrimitive
                       || type.IsEnum
                       || type.Equals(typeof(string))
                       || type.Equals(typeof(decimal));
        }

        private static bool IsCollection(PropertyInfo property)
            => typeof(IEnumerable).IsAssignableFrom(property.PropertyType);

        private bool IsCollectionWriteable(IEnumerable collection)
            => collection?.Cast<object>().FirstOrDefault() != null;
    }
}
