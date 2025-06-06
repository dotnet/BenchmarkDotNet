using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Models;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Tests.Builders;
using BenchmarkDotNet.Tests.Infra;
using BenchmarkDotNet.Tests.Perfonar.Infra;
using JetBrains.Annotations;
using Perfolizer.Models;
using Perfolizer.Json;
using Perfolizer.Perfonar.Base;
using Perfolizer.Perfonar.Tables;
using VerifyXunit;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.Tests.Perfonar;

public class PerfonarTests(ITestOutputHelper output)
{
    [Theory]
    [MemberData(nameof(EntryDataKeys))]
    public Task PerfonarIndexTest(string key) => VerifyString(key, new PerfonarIndex(TableDataMap[key].RootEntry).Dump());

    [Theory]
    [MemberData(nameof(EntryDataKeys))]
    public Task PerfonarTableTest(string key)
    {
        var table = TableDataMap[key];
        string markdown = table.ToMarkdown(new PerfonarTableStyle());
        string json = LightJsonSerializer.Serialize(table.RootEntry, new LightJsonSettings { Indent = true });
        return VerifyString(key, markdown + "\n" + json);
    }

    private static readonly IDictionary<string, PerfonarTable> TableDataMap = new Dictionary<string, PerfonarTable>
    {
        {
            "default01", new PerfonarTable(
                Root().Add(
                    Benchmark("Foo", "10ns", "11ns", "12ns"),
                    Benchmark("Bar", "200ns", "201ns", "202ns")
                ),
                GetDefaultTableConfig()
            )
        },
        {
            "default02", new PerfonarTable(
                Root().Add(
                    Job(RuntimeMoniker.Net481).Add(
                        Benchmark("Foo", "10ns", "11ns", "12ns"),
                        Benchmark("Bar", "20ns", "21ns", "22ns")),
                    Job(RuntimeMoniker.Net70).Add(
                        Benchmark("Foo", "30ns", "31ns", "32ns"),
                        Benchmark("Bar", "40ns", "41ns", "42ns"))),
                GetDefaultTableConfig()
            )
        },
        {
            "default03", new PerfonarTable(
                Root().Add(
                    Job(RuntimeMoniker.Net70, Jit.RyuJit).Add(
                        Benchmark("Foo", "30ns", "31ns", "32ns"),
                        Benchmark("Bar", "40ns", "41ns", "42ns"))),
                GetDefaultTableConfig()
            )
        },
        {
            "default04", new PerfonarTable(
                Root().Add(
                    Job(RuntimeMoniker.Net481, Jit.LegacyJit).Add(
                        Benchmark("Foo", "10ns", "11ns", "12ns"),
                        Benchmark("Bar", "20ns", "21ns", "22ns")),
                    Job(RuntimeMoniker.Net70, Jit.RyuJit).Add(
                        Benchmark("Foo", "30ns", "31ns", "32ns"),
                        Benchmark("Bar", "40ns", "41ns", "42ns"))),
                GetDefaultTableConfig()
            )
        },
        {
            "default05", new PerfonarTable(
                Root().Add(
                    Enumerable.Range(0, 20).Select(index =>
                        Job((RuntimeMoniker)index, Jit.RyuJit, index).Add(
                            Benchmark("Foo", index * 10 + 1 + "ns", index * 10 + 2 + "ns", index * 10 + 3 + "ns"),
                            Benchmark("Bar", index * 10 + 6 + "ns", index * 10 + 7 + "ns", index * 10 + 8 + "ns")
                        )).ToArray()),
                GetDefaultTableConfig()
            )
        },
        {
            "sort01", new PerfonarTable(
                new EntryInfo().Add(Benchmark("Foo", "10ms"), Benchmark("Bar", "20ms")),
                new PerfonarTableConfig
                {
                    ColumnDefinitions =
                    {
                        new PerfonarColumnDefinition(".benchmark.method"),
                        new PerfonarColumnDefinition("=center")
                    },
                    SortPolicies =
                    [
                        new PerfonarSortPolicy("=center", PerfonarSortDirection.Descending)
                    ]
                })
        },
        {
            "params01", new PerfonarTable(
                new EntryInfo().Add(
                    new EntryInfo
                    {
                        Parameters = new Dictionary<string, object> { { "A", 1 }, { "B", 2 } }
                    }.Add(Benchmark("Foo", "10ms"))).Add(
                    new EntryInfo
                    {
                        Parameters = new Dictionary<string, object> { { "A", 10 }, { "B", 20 } }
                    }.Add(Benchmark("Bar", "20ms"))),
                new PerfonarTableConfig
                {
                    ColumnDefinitions =
                    {
                        new PerfonarColumnDefinition(".benchmark.method"),
                        new PerfonarColumnDefinition(".parameters"),
                        new PerfonarColumnDefinition("=center")
                    }
                }
            )
        }
    };

    [UsedImplicitly] public static TheoryData<string> EntryDataKeys = TheoryDataHelper.Create(TableDataMap.Keys);

    private static EntryInfo Root() => new EntryInfo
    {
        Engine = PerfonarMock.Engine,
        Host = new HostEnvironmentInfoBuilder().Build().ToPerfonar()
    };

    private static PerfonarTableConfig GetDefaultTableConfig() => new ()
    {
        ColumnDefinitions =
        [
            new PerfonarColumnDefinition(".engine") { Cloud = "primary", IsSelfExplanatory = true, IsAtomic = true },
            new PerfonarColumnDefinition(".host.os") { Cloud = "primary", IsSelfExplanatory = true, IsAtomic = true },
            new PerfonarColumnDefinition(".host.cpu") { Cloud = "primary", IsSelfExplanatory = true, IsAtomic = true },
            new PerfonarColumnDefinition(".benchmark") { Cloud = "secondary" },
            new PerfonarColumnDefinition(".job") { Cloud = "secondary", Compressed = true },
            new PerfonarColumnDefinition("=center"),
            new PerfonarColumnDefinition("=spread")
        ]
    };

    private static EntryInfo Job(RuntimeMoniker? runtime = null, Jit? jit = null, int? affinity = null) => new EntryInfo
    {
        Job = new JobInfo
        {
            Environment = new BdnEnvironment { Runtime = runtime, Jit = jit, Affinity = affinity }
        }
    };

    private static EntryInfo Benchmark(string name, params string[] metrics) => new EntryInfo
    {
        Benchmark = new BdnBenchmark { Type = "Bench", Method = name }
    }.AddMetrics(metrics);

    private Task VerifyString(string key, string content)
    {
        output.WriteLine(content);
        var settings = VerifyHelper.Create("Perfonar");
        settings.UseParameters(key);
        return Verifier.Verify(content, settings);
    }
}