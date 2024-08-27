using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Phd;
using BenchmarkDotNet.Tests.Builders;
using BenchmarkDotNet.Tests.Infra;
using BenchmarkDotNet.Tests.Phd.Infra;
using JetBrains.Annotations;
using Perfolizer.Json;
using Perfolizer.Phd.Base;
using Perfolizer.Phd.Dto;
using Perfolizer.Phd.Presenting;
using Perfolizer.Phd.Tables;
using Perfolizer.Presenting;
using VerifyXunit;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.Tests.Phd;

[UsesVerify]
public class PhdTests(ITestOutputHelper output)
{
    [Theory]
    [MemberData(nameof(EntryDataKeys))]
    public Task PhdIndexTest(string key) => VerifyString(key, new PhdIndex(EntryDataMap[key]).Dump());

    [Theory]
    [MemberData(nameof(EntryDataKeys))]
    public Task PhdTableTest(string key)
    {
        var entry = EntryDataMap[key];
        var table = new PhdTable(entry);
        var presenter = new StringPresenter();
        new PhdMarkdownTablePresenter(presenter).Present(table, new PhdTableStyle());
        string json = LightJsonSerializer.Serialize(entry, new LightJsonSettings { Indent = true });
        return VerifyString(key, presenter.Dump() + "\n" + json);
    }

    private static readonly IDictionary<string, PhdEntry> EntryDataMap = new Dictionary<string, PhdEntry>
    {
        {
            "default01", Root().Add(
                Benchmark("Foo", "10ns", "11ns", "12ns"),
                Benchmark("Bar", "200ns", "201ns", "202ns")
            )
        },
        {
            "default02", Root().Add(
                Job(RuntimeMoniker.Net481).Add(
                    Benchmark("Foo", "10ns", "11ns", "12ns"),
                    Benchmark("Bar", "20ns", "21ns", "22ns")),
                Job(RuntimeMoniker.Net70).Add(
                    Benchmark("Foo", "30ns", "31ns", "32ns"),
                    Benchmark("Bar", "40ns", "41ns", "42ns")))
        },
        {
            "default03", Root().Add(
                Job(RuntimeMoniker.Net70, Jit.RyuJit).Add(
                    Benchmark("Foo", "30ns", "31ns", "32ns"),
                    Benchmark("Bar", "40ns", "41ns", "42ns")))
        },
        {
            "default04", Root().Add(
                Job(RuntimeMoniker.Net481, Jit.LegacyJit).Add(
                    Benchmark("Foo", "10ns", "11ns", "12ns"),
                    Benchmark("Bar", "20ns", "21ns", "22ns")),
                Job(RuntimeMoniker.Net70, Jit.RyuJit).Add(
                    Benchmark("Foo", "30ns", "31ns", "32ns"),
                    Benchmark("Bar", "40ns", "41ns", "42ns")))
        },
        {
            "default05", Root().Add(
                Enumerable.Range(0, 20).Select(index =>
                    Job((RuntimeMoniker)index, Jit.RyuJit, index).Add(
                        Benchmark("Foo", index * 10 + 1 + "ns", index * 10 + 2 + "ns", index * 10 + 3 + "ns"),
                        Benchmark("Bar", index * 10 + 6 + "ns", index * 10 + 7 + "ns", index * 10 + 8 + "ns")
                    )).ToArray())
        },
        {
            "sort01", new PhdEntry
            {
                Meta = new PhdMeta
                {
                    Table = new PhdTableConfig
                    {
                        ColumnDefinitions =
                        {
                            new PhdColumnDefinition(".benchmark.method"),
                            new PhdColumnDefinition("=center")
                        },
                        SortPolicies =
                        [
                            new PhdSortPolicy("=center", PhdSortDirection.Descending)
                        ]
                    }
                }
            }.Add(Benchmark("Foo", "10ms"), Benchmark("Bar", "20ms"))
        },
        {
            "params01", new PhdEntry
            {
                Meta = new PhdMeta
                {
                    Table = new PhdTableConfig
                    {
                        ColumnDefinitions =
                        {
                            new PhdColumnDefinition(".benchmark.method"),
                            new PhdColumnDefinition(".parameters"),
                            new PhdColumnDefinition("=center")
                        }
                    }
                }
            }.Add(
                new PhdEntry
                {
                    Parameters = new Dictionary<string, object> { { "A", 1 }, { "B", 2 } }
                }.Add(Benchmark("Foo", "10ms"))).Add(
                new PhdEntry
                {
                    Parameters = new Dictionary<string, object> { { "A", 10 }, { "B", 20 } }
                }.Add(Benchmark("Bar", "20ms")))
        }
    };

    [UsedImplicitly] public static TheoryData<string> EntryDataKeys = TheoryDataHelper.Create(EntryDataMap.Keys);

    private static PhdEntry Root() => new PhdEntry
    {
        Meta = new PhdMeta
        {
            Table = new PhdTableConfig
            {
                ColumnDefinitions =
                [
                    new PhdColumnDefinition(".engine") { Cloud = "primary", IsSelfExplanatory = true, IsAtomic = true },
                    new PhdColumnDefinition(".host.os") { Cloud = "primary", IsSelfExplanatory = true, IsAtomic = true },
                    new PhdColumnDefinition(".host.cpu") { Cloud = "primary", IsSelfExplanatory = true, IsAtomic = true },
                    new PhdColumnDefinition(".benchmark") { Cloud = "secondary" },
                    new PhdColumnDefinition(".job") { Cloud = "secondary", Compressed = true },
                    new PhdColumnDefinition("=center"),
                    new PhdColumnDefinition("=spread")
                ]
            }
        },
        Engine = PhdMock.Engine,
        Host = new HostEnvironmentInfoBuilder().Build().ToPhd()
    };

    private static PhdEntry Job(RuntimeMoniker? runtime = null, Jit? jit = null, int? affinity = null) => new PhdEntry
    {
        Job = new PhdJob
        {
            Environment = new BdnEnvironment { Runtime = runtime, Jit = jit, Affinity = affinity }
        }
    };

    private static PhdEntry Benchmark(string name, params string[] metrics) => new PhdEntry
    {
        Benchmark = new BdnBenchmark { Type = "Bench", Method = name }
    }.AddMetrics(metrics);

    private Task VerifyString(string key, string content)
    {
        output.WriteLine(content);
        var settings = VerifyHelper.Create("Phd");
        settings.UseParameters(key);
        return Verifier.Verify(content, settings);
    }
}