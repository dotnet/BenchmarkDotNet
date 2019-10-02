using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using BenchmarkDotNet.Exporters.Xml;
using JetBrains.Annotations;
using Xunit;

namespace BenchmarkDotNet.Tests.Exporters
{
    public class XmlSerializerTests
    {
        [Fact]
        public void Serialize()
        {
            string expected =
                "Start " +
                "MockSource " +
                    "IsValid True IsValid "+
                    "IntNumber 42 IntNumber " +
                    $"DoubleNumber {double.MaxValue.ToString(CultureInfo.InvariantCulture)} DoubleNumber " +
                    $"LongNumber {long.MaxValue} LongNumber " +
                    "Name Source Name " +
                    "Items " +
                        "Item Name i1 Name Item " +
                        "Item Name i2 Name Item " +
                    "Items " +
                    "DoubleArray " +
                        "Item 1 Item " +
                        "Item 2 Item " +
                    "DoubleArray " +
                "MockSource " +
                "End";
            var source = new MockSource();
            var writer = new MockXmlWriter();
            IXmlSerializer serializer = XmlSerializer.GetBuilder(typeof(MockSource)).Build();

            serializer.Serialize(writer, source);
            string actual = writer.ToString();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void WithRootName()
        {
            var source = new MockSource();
            var writer = new MockXmlWriter();
            string rootName = "CustomRoot";
            IXmlSerializer serializer = XmlSerializer.GetBuilder(typeof(MockSource))
                                                .WithRootName(rootName)
                                                .Build();

            serializer.Serialize(writer, source);
            string actual = writer.ToString();

            Assert.Contains(rootName, actual);
            Assert.DoesNotContain(nameof(MockSource), actual);
        }

        [Fact]
        public void WithCollectionItemName()
        {
            var source = new MockSource();
            var writer = new MockXmlWriter();
            var itemName = "CustomItem";
            IXmlSerializer serializer = XmlSerializer.GetBuilder(typeof(MockSource))
                                                .WithCollectionItemName(nameof(MockSource.Items), itemName)
                                                .Build();

            serializer.Serialize(writer, source);
            string actual = writer.ToString();

            Assert.Contains(itemName, actual);
            Assert.DoesNotContain(nameof(XmlSerializer.DefaultItemName), actual);
        }

        [Fact]
        public void WithExcludedProperty()
        {
            var source = new MockSource();
            var writer = new MockXmlWriter();
            var excludedProperty = nameof(MockSource.IntNumber);
            IXmlSerializer serializer = XmlSerializer.GetBuilder(typeof(MockSource))
                                                .WithExcludedProperty(excludedProperty)
                                                .Build();

            serializer.Serialize(writer, source);
            string actual = writer.ToString();

            Assert.DoesNotContain(excludedProperty, actual);
        }

        [Fact]
        public void CtorThrowsWhenParameterIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => XmlSerializer.GetBuilder(null).Build());
        }

        [Theory]
        [InlineData(null, typeof(ArgumentException))]
        [InlineData(" ", typeof(ArgumentException))]
        [InlineData("", typeof(ArgumentException))]
        public void WithRootNameThrowsGivenNameIsNullOrWhiteSpace(string name, Type exception)
        {
            Assert.Throws(exception, () => XmlSerializer.GetBuilder(typeof(MockSource))
                                                    .WithRootName(name)
                                                    .Build());
        }

        [Theory]
        [InlineData(null, null, typeof(ArgumentException))]
        [InlineData(nameof(MockSource.Items), null, typeof(ArgumentException))]
        [InlineData(nameof(MockSource.Items), " ", typeof(ArgumentException))]
        [InlineData(nameof(MockSource.Items), "", typeof(ArgumentException))]
        [InlineData(null, "MockItem", typeof(ArgumentException))]
        [InlineData(" ", "MockItem", typeof(ArgumentException))]
        [InlineData("", "MockItem", typeof(ArgumentException))]
        public void WithCollectionItemNameThrowsGivenInvalidArguments(string collectionName, string itemName, Type exception)
        {
            Assert.Throws(exception, () => XmlSerializer.GetBuilder(typeof(MockSource))
                                                    .WithCollectionItemName(collectionName, itemName)
                                                    .Build());
        }

        [Theory]
        [InlineData(null, typeof(ArgumentException))]
        [InlineData(" ", typeof(ArgumentException))]
        [InlineData("", typeof(ArgumentException))]
        public void WithExcludedPropertyThrowsGivenNameIsNullOrWhiteSpace(string name, Type exception)
        {
            Assert.Throws(exception, () => XmlSerializer.GetBuilder(typeof(MockSource))
                                                    .WithExcludedProperty(name)
                                                    .Build());
        }

        [Theory]
        [MemberData(nameof(SerializeTestDataNames))]
        public void SerializeThrowsGivenNullArguments(string dataName)
        {
            var (writer, source, exception) = SerializeTestDataItems[dataName];
            var serializer = XmlSerializer.GetBuilder(typeof(MockSource)).Build();
            Assert.Throws(exception, () => serializer.Serialize(writer, source));
        }

        private class SerializeTestData
        {
            private MockXmlWriter Writer { get; }
            private object Source { get; }
            private Type Exception { get; }

            public SerializeTestData(MockXmlWriter writer, object source, Type exception)
            {
                Writer = writer;
                Source = source;
                Exception = exception;
            }

            public void Deconstruct(out MockXmlWriter writer, out object source, out Type exception)
            {
                writer = Writer;
                source = Source;
                exception = Exception;
            }
        }

        private static readonly Dictionary<string, SerializeTestData> SerializeTestDataItems = new Dictionary<string, SerializeTestData>
        {
            {"Null", new SerializeTestData(null, null, typeof(ArgumentNullException))},
            {"MockSource", new SerializeTestData(null, new MockSource(), typeof(ArgumentNullException))},
            {"MockXmlWriter", new SerializeTestData(new MockXmlWriter(), null, typeof(ArgumentNullException))}
        };

        [UsedImplicitly]
        public static TheoryData<string> SerializeTestDataNames => TheoryDataHelper.Create(SerializeTestDataItems.Keys);

        [Fact]
        public void WritesElementStringGivenSimpleCollectionItem()
        {
            IXmlWriter writer = new MockXmlWriter();
            IXmlSerializer serializer = XmlSerializer.GetBuilder(typeof(SimpleItemSource)).Build();

            serializer.Serialize(writer, new SimpleItemSource());
            string actual = writer.ToString();

            Assert.Contains("Item s1 Item", actual);
            Assert.Contains("Item s2 Item", actual);
        }

        [Fact]
        public void DoesNotWriteUnwriteableCollection()
        {
            IXmlWriter writer = new MockXmlWriter();
            IXmlSerializer serializer = XmlSerializer.GetBuilder(typeof(UnwriteableCollectionSource)).Build();

            serializer.Serialize(writer, new UnwriteableCollectionSource());
            string actual = writer.ToString();

            Assert.DoesNotContain(actual, "Items");
        }

        private class MockSource
        {
            public bool IsValid { get; } = true;
            public int IntNumber { get; } = 42;
            public double DoubleNumber { get; } = double.MaxValue;
            public long LongNumber { get; } = long.MaxValue;
            public string Name { get; } = "Source";
            public IEnumerable<MockCollectionItem> Items { get; }
                = new List<MockCollectionItem>
                {
                    new MockCollectionItem("i1"), new MockCollectionItem("i2")
                };
            public double[] DoubleArray { get; } = new double[] { 1, 2 };
        }

        private class SimpleItemSource
        {
            public IEnumerable<string> Items { get; } = new List<string> { "s1", "s2" };
        }

        private class UnwriteableCollectionSource
        {
            public IEnumerable<string> Items { get; }
        }

        private class MockCollectionItem
        {
            public string Name { get; }

            public MockCollectionItem(string name) { Name = name;  }
        }

        public class MockXmlWriter : IXmlWriter
        {
            private readonly StringBuilder writer = new StringBuilder();
            private readonly Stack<string> openElements = new Stack<string>();

            public void WriteStartDocument()
            {
                writer.Append("Start ");
            }

            public void WriteElementString(string localName, string value)
            {
                writer.Append(localName);
                writer.Append(" ");
                writer.Append(value);
                writer.Append(" ");
                writer.Append(localName);
                writer.Append(" ");
            }

            public void WriteEndDocument()
            {
                writer.Append("End");
            }

            public void WriteEndElement()
            {
                var endElement = openElements.Pop();
                writer.Append(endElement);
                writer.Append(" ");
            }

            public void WriteStartElement(string localName)
            {
                openElements.Push(localName);
                writer.Append(localName);
                writer.Append(" ");
            }

            public override string ToString() => writer.ToString();
        }
    }
}
