using AwesomeAssertions;
using BenchmarkDotNet.Serialization;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace BenchmarkDotNet.Tests;

public class SummarySerializationTests
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
            data[i] = ReplaceUnsupportedNumericValues(data[i]);
        }

        //Assert
        Assert.Equal(val1, data[0]);
        Assert.Equal(val2, data[1]);
        Assert.Equal(JSON_EMPTY_STRING, data[2]);
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
        string json = BdnSimpleJsonSerializer.Serialize(data, indentJson: true);

        //Assert
        var expected =
            """
            {
              "Statistic1": "",
              "Statistic2": "",
              "Statistic3": "",
              "Statistic4": "",
              "Statistic5": "",
              "Statistic6": ""
            }
            """;
        Assert.Equal(expected, json);
    }

    private const string JSON_EMPTY_STRING = "\"\"";

    private static object ReplaceUnsupportedNumericValues(object value)
    {
        switch (value)
        {
            case float.NaN:
            case float.NegativeInfinity:
            case float.PositiveInfinity:
            case double.NaN:
            case double.NegativeInfinity:
            case double.PositiveInfinity:
                return JSON_EMPTY_STRING;
            default:
                return value;
        }
    }
}
