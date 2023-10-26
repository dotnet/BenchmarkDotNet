using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace BenchmarkDotNet.Exporters.Xml
{
    internal class XmlSerializer : IXmlSerializer
    {
        private readonly Type type;
        private readonly string rootName;
        private readonly IReadOnlyDictionary<string, string> collectionItemNameMap;
        private readonly IReadOnlyCollection<string> excludedPropertyNames;

        private IXmlWriter writer;

        public const string DefaultItemName = "Item";

        private XmlSerializer(XmlSerializerBuilder builder)
        {
            type = builder.Type;
            rootName = builder.RootName;
            collectionItemNameMap = builder.CollectionItemNameMap;
            excludedPropertyNames = builder.ExcludedPropertyNames;
        }

        public static XmlSerializerBuilder GetBuilder(Type type) => new XmlSerializerBuilder(type);

        public void Serialize(IXmlWriter newWriter, object source)
        {
            if (source == null || source.GetType() != type)
                throw new ArgumentNullException(nameof(source));

            writer = newWriter ?? throw new ArgumentNullException(nameof(newWriter));

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
            if (source == null || excludedPropertyNames.Contains(property.Name))
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

        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        private void WriteCollectionProperty(object source, PropertyInfo property)
        {
            var collection = (IEnumerable)property.GetValue(source);

            if (!IsCollectionWritable(collection))
                return;

            writer.WriteStartElement(property.Name);

            string? itemName = null;

            foreach (var item in collection)
            {
                if (itemName == null)
                {
                    itemName = collectionItemNameMap.ContainsKey(property.Name) ? collectionItemNameMap[property.Name] : DefaultItemName;
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
                       || type == typeof(string)
                       || type == typeof(decimal);
        }

        private static bool IsCollection(PropertyInfo property)
            => typeof(IEnumerable).IsAssignableFrom(property.PropertyType);

        private static bool IsCollectionWritable(IEnumerable collection)
            => collection?.Cast<object>().FirstOrDefault() != null;

        internal class XmlSerializerBuilder
        {
            private readonly Dictionary<string, string> collectionItemNameMap = new Dictionary<string, string>();
            private readonly HashSet<string> excludedPropertyNames = new HashSet<string>();

            public Type Type { get; }
            public string RootName { get; private set; }
            public IReadOnlyDictionary<string, string> CollectionItemNameMap => collectionItemNameMap;
            public IReadOnlyCollection<string> ExcludedPropertyNames => excludedPropertyNames;

            public XmlSerializerBuilder(Type type)
            {
                if (type == null)
                    throw new ArgumentNullException(nameof(type));

                Type = type;
                RootName = type.Name;
            }

            public XmlSerializerBuilder WithRootName(string rootName)
            {
                if (string.IsNullOrWhiteSpace(rootName))
                    throw new ArgumentException(nameof(rootName));

                RootName = rootName;
                return this;
            }

            public XmlSerializerBuilder WithCollectionItemName(string collectionName, string itemName)
            {
                if (string.IsNullOrWhiteSpace(collectionName))
                    throw new ArgumentException(nameof(collectionName));
                if (string.IsNullOrWhiteSpace(itemName))
                    throw new ArgumentException(nameof(itemName));

                collectionItemNameMap.Add(collectionName, itemName);
                return this;
            }

            public XmlSerializerBuilder WithExcludedProperty(string propertyName)
            {
                if (string.IsNullOrWhiteSpace(propertyName))
                    throw new ArgumentException(nameof(propertyName));

                excludedPropertyNames.Add(propertyName);
                return this;
            }

            public IXmlSerializer Build() => new XmlSerializer(this);
        }
    }
}
