using AsmArm64;
using AwesomeAssertions;
using AwesomeAssertions.Equivalency;
using BenchmarkDotNet.Disassemblers;
using BenchmarkDotNet.Serialization;
using BenchmarkDotNet.Tests.Disassemblers;
using BenchmarkDotNet.Tests.Disassemblers.Arm64;
using Iced.Intel;
using static AsmArm64.Arm64RegisterSP;
using static AsmArm64.Arm64RegisterW;
using static AsmArm64.Arm64RegisterX;

namespace BenchmarkDotNet.Tests;

public class DisassemblerModelSerializationTests(ITestOutputHelper output) : Arm64DisassemblerTestBase(output)
{
    // Check Arm64Instruction equivalency by ToString output.
    private static EquivalencyOptions<Arm64Asm> ConfigureCustomEquivalency(EquivalencyOptions<Arm64Asm> options)
        => options
            .Using<Arm64Instruction>(ctx => ctx.Subject.ToString().Should().Be(ctx.Expectation.ToString()))
            .When(info => info.Path == "Instruction");

    [Fact]
    public void ClrMdArgsSerializationTest()
    {
        // Arrange
        var model = new ClrMdArgs(
            processId: 100, //  ProcessID field is not serialized/deserialized.
            typeName: "TypeName", //  TypeName field is not serialized/deserialized.
            methodName: "MethodName",
            printSource: true,
            maxDepth: 5,
            syntax: "Syntax",
            runtimeVersion: new Version(8, 0),
            filters: ["filter1", "filter2"],
            resultsPath: "/path/to/results"
        );

        // Act
        var json = BdnJsonSerializer.Serialize(model);
        var result = BdnJsonSerializer.Deserialize<ClrMdArgs>(json);

        // Assert
        json.Should().NotBe("{}");
        result.Should().BeEquivalentTo(
            model,
            options => options
                .IncludingInternalFields()
                .ComparingByMembers<ClrMdArgs>() // Required to use Excluding for struct type. See: https://github.com/fluentassertions/fluentassertions/issues/937
                .Excluding(x => x.ProcessId)
                .Excluding(x => x.TypeName));
    }

    [Fact]
    public void SharpSerializationTest()
    {
        var model = new Sharp
        {
            InstructionPointer = 1,
            Text = "Text",
            FilePath = "FilePath",
            LineNumber = 2,
        };

        // Act
        var json = BdnJsonSerializer.Serialize(model);
        var result = BdnJsonSerializer.Deserialize<Sharp>(json);

        // Assert
        json.Should().NotBe("{}");
        result.Should().BeEquivalentTo(model);
    }

    [Fact]
    public void MonoCodeSerializationTest()
    {
        // Arrange
        var model = new MonoCode
        {
            InstructionPointer = 1,
            Text = "Text",
        };

        // Act
        var json = BdnJsonSerializer.Serialize(model);
        var result = BdnJsonSerializer.Deserialize<MonoCode>(json);

        // Assert
        json.Should().NotBe("{}");
        result.Should().BeEquivalentTo(model);
    }

    [Fact]
    public void IntelAsmSerializationTest()
    {
        // Arrange
        var model = new IntelAsm
        {
            InstructionPointer = 1,
            InstructionLength = 2,
            ReferencedAddress = 3,
            IsReferencedAddressIndirect = true,
            Instruction = Instruction.Create(
                Iced.Intel.Code.Xlat_m8,
                new MemoryOperand(Register.RBX, Register.AL)
            ),
        };

        // Act
        var json = BdnJsonSerializer.Serialize(model);
        var result = BdnJsonSerializer.Deserialize<IntelAsm>(json);

        // Assert
        Assert.NotEqual("{}", json);
        Assert.Equivalent(model, result, strict: true);
    }

    [Fact]
    public void Arm64AsmSerializationTest()
    {
        // Arrange
        var rawInstruction = Arm64InstructionFactory.LDR(W1, new Arm64ImmediateMemoryAccessor(SP, 8)); // ldr w1, [sp, #8]
        byte[] instructionBytes = rawInstruction.ToLittleEndianBytes();

        using var clrRuntime = new MockMemory().ToMockClrRuntime();
        var state = new State(clrRuntime, DummyTargetFrameworkVersion);

        var helper = new Arm64DisassemblerHelper();

        // Act
        var instructions = helper.Decode(instructionBytes, 0, state, 0, DummyCurrentMethod, Diagnosers.DisassemblySyntax.Att);
        var instruction = instructions.Single();

        var model = new Arm64Asm
        {
            InstructionPointer = instruction.InstructionPointer,
            RawInstruction = instruction.RawInstruction,
            DisassembleSyntax = instruction.DisassembleSyntax,
            ReferencedAddress = (instruction.ReferencedAddress > ushort.MaxValue) ? instruction.ReferencedAddress : null,
            IsReferencedAddressIndirect = true, // Test with dummy value
        };

        // Act
        var json = BdnJsonSerializer.Serialize(model);
        var result = BdnJsonSerializer.Deserialize<Arm64Asm>(json);

        // Assert
        json.Should().NotBe("{}");

        // Compare properties
        result.Should().NotBeNull();
        result.Instruction.Should().NotBeNull();
        result.Instruction.ToString().Should().Be("ldr w1, [sp, #8]");

        result.Should().BeEquivalentTo(model, ConfigureCustomEquivalency);
    }

    [Fact]
    public void MapSerializationTest()
    {
        // Arrange
        var model = new Map
        {
            SourceCodes =
            [
                new MonoCode
                {
                    Text = "MonoCodeText1",
                    InstructionPointer = 1,
                },
                new Sharp
                {
                    Text = "SharpText" ,
                    FilePath ="FilePath",
                    LineNumber = 1,
                    InstructionPointer = 2,
                },
                new MonoCode {
                    Text = "MonoCodeText2",
                    InstructionPointer = 2,
                },
            ]
        };

        // Act
        var json = BdnJsonSerializer.Serialize(model);
        var result = BdnJsonSerializer.Deserialize<Map>(json);

        // Assert
        json.Should().NotBe("{}");
        result.Should().BeEquivalentTo(model);
    }

    [Fact]
    public void DisassembledMethodSerializationTest()
    {
        // Arrange
        var model = new DisassembledMethod
        {
            Name = "Name",
            CommandLine = "CommandLine",
            NativeCode = 1,
            Problem = "Problem",
            Maps =
            [
                new Map()
                {
                    SourceCodes =
                    [
                        new MonoCode
                        {
                            InstructionPointer = 1,
                            Text = "MonoCode1",
                        },
                    ]
                },
                new Map()
                {
                    SourceCodes =
                    [
                        new MonoCode
                        {
                            InstructionPointer = 2,
                            Text = "MonoCode2",
                        },
                    ]
                },
            ]
        };

        // Act
        var json = BdnJsonSerializer.Serialize(model);
        var result = BdnJsonSerializer.Deserialize<DisassembledMethod>(json);

        // Assert
        json.Should().NotBe("{}");
        result.Should().BeEquivalentTo(model);
    }

    [Fact]
    public void DisassemblyResultSerializationTest()
    {
        var model = new DisassemblyResult
        {
            AddressToNameMapping =
            {
                [1] = "Name1",
                [2] = "Name2",
                [3] = "Name3",
            },
            Errors = ["Error1", "Error2", "Error3"],
            Methods =
            [
                new DisassembledMethod{Name= "Method1" },
                new DisassembledMethod{Name= "Method2" },
                new DisassembledMethod{Name= "Method3" },
            ],
            PointerSize = 1,
        };

        // Act
        var json = BdnJsonSerializer.Serialize(model);
        var result = BdnJsonSerializer.Deserialize<DisassemblyResult>(json);

        // Assert
        Assert.NotEqual("{}", json);
        Assert.Equivalent(model, result, strict: true);
    }
}
