﻿using System.Collections.Generic;
using System.Linq;
using SimpleJson;
using Xunit;

namespace BenchmarkDotNet.Tests
{
    public class LightJsonSerializerTests
    {
        [Theory]
        [InlineData(10.0, "test", double.NaN)]
        public void SimpleJson_ReplaceUnsupportedNumericValues_Smoke(object val1, object val2, object val3)
        {
            //Arrange
            var data = new List<object>()
            {
                val1, val2, val3
            };

            //Act
            for (int i = 0; i < data.Count; i++)
            {
                data[i] = SimpleJsonSerializer.ReplaceUnsupportedNumericValues(data[i]);
            }

            //Assert
            Assert.Equal(val1, data[0]);
            Assert.Equal(val2, data[1]);
            Assert.Equal(SimpleJsonSerializer.JSON_EMPTY_STRING, data[2]);
        }


        [Fact]
        public void SimpleJson_SerializeObjectWithUnsupportedNumericValues_ReturnsValidJson()
        {
            //Arrange
            var data = new Dictionary<string, object>
            {
                { "Statistic1", float.NaN},
                { "Statistic2", float.NegativeInfinity },
                { "Statistic3", float.PositiveInfinity },
                { "Statistic4", double.NaN},
                { "Statistic5", double.NegativeInfinity },
                { "Statistic6", double.PositiveInfinity }
            };

            //Act
            string json = SimpleJsonSerializer.SerializeObject(data);

            //Assert
            Assert.True(SimpleJsonSerializer.TryDeserializeObject(json, out var obj));
            var values = (obj as SimpleJson.JsonObject).Select(x => x.Value);
            Assert.True(values.All(x => x.Equals(string.Empty)));
        }
    }
}
