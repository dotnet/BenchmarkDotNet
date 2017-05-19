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
        private XmlWriter writer;
        private Dictionary<Type, string> itemNames = new Dictionary<Type, string>();
        private string rootName;

        public XmlSerializer(Type type)
        {
            this.type = type;
            rootName = type.Name;
        }

        public XmlSerializer WithRootName(string rootName)
        {
            this.rootName = rootName;
            return this;
        }

        public XmlSerializer WithCollectionItemName(Type type, string name)
        {
            itemNames.Add(type, name);
            return this;
        }

        public void Serialize(XmlWriter writer, object source)
        {
            this.writer = writer;

            writer.WriteStartDocument();

            WriteRoot(source, rootName);

            writer.WriteEndDocument();
        }

        private void WriteProperty(object source, PropertyInfo property)
        {
            if (source == null)
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

        private void WriteRoot(object source, string elementName)
        {
            writer.WriteStartElement(elementName);

            foreach (var property in source.GetType().GetProperties())
            {
                WriteProperty(source, property);
            }

            writer.WriteEndElement();
        }

        private void WriteCollectionProperty(object source, PropertyInfo property)
        {
            IEnumerable collection = (IEnumerable)property.GetValue(source);

            if (IsCollectionWriteable(collection))
            {
                writer.WriteStartElement(property.Name);

                string itemName = null;

                foreach (var item in collection)
                {
                    if (itemName == null)
                    {
                        // Item name can be retrieved from the collection's source object
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