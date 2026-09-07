using AsmArm64;
using Gee.External.Capstone;
using Gee.External.Capstone.Arm64;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Arm64Instruction = Gee.External.Capstone.Arm64.Arm64Instruction;

namespace BenchmarkDotNet.Tests.Disassemblers;

internal static class Arm64TestInstructions
{
    public static Arm64Instruction Movz(Arm64RegisterX register, ushort value, int shift = 0)
        => Arm64InstructionFactory.MOVZ(register, value, amount: shift).ToCapstoneArm64Instruction();

    public static Arm64Instruction Movk(Arm64RegisterX register, ushort value, int shift = 0)
        => Arm64InstructionFactory.MOVK(register, value, amount: shift).ToCapstoneArm64Instruction();

    public static Arm64Instruction Add(Arm64RegisterX destination, Arm64RegisterX source, ushort immediate)
        => Arm64InstructionFactory.ADD(destination, source, immediate).ToCapstoneArm64Instruction();

    public static Arm64Instruction Add(Arm64RegisterX destination, Arm64RegisterX source, ushort immediate, byte shiftAmount)
        => Arm64InstructionFactory.ADD(destination, source, immediate, amount: shiftAmount).ToCapstoneArm64Instruction();

    public static Arm64Instruction Adrp(Arm64RegisterX register, Arm64LabelOffset offset)
    {
        if (offset.Value % 4096 != 0)
            throw new ArgumentException("ADRP label offset must be a multiple of 4096", nameof(offset));

        return Arm64InstructionFactory.ADRP(register, offset).ToCapstoneArm64Instruction();
    }

    public static Arm64Instruction Ldr(Arm64RegisterX destination, Arm64RegisterX baseRegister, short immediate = 0)
    {
        var memoryAccessor = new Arm64ImmediateMemoryAccessor(baseRegister, immediate); // Use unsigned offset
        return Arm64InstructionFactory.LDR(destination, memoryAccessor).ToCapstoneArm64Instruction(); // ldr Xt, [Xn]
    }

    public static Arm64Instruction Ldr(Arm64RegisterX destination, Arm64RegisterX baseRegister, Arm64RegisterX indexRegister)
    {
        var dummyMemoryExtend = new Arm64MemoryExtend();  // Use dummy IArm64MemoryExtend. Because it's not used.
        var memoryAccessor = new Arm64RegisterXExtendMemoryAccessor(baseRegister, indexRegister, dummyMemoryExtend);
        return Arm64InstructionFactory.LDR(destination, memoryAccessor).ToCapstoneArm64Instruction();
    }

    // LDR with PC(Program Counter) relative offset.
    public static Arm64Instruction Ldr(Arm64RegisterX destination, Arm64LabelOffset label)
    {
        label.ValidateMultipleOf8();
        return Arm64InstructionFactory.LDR(destination, label).ToCapstoneArm64Instruction(); // ldr Xd, [Xn, #label]
    }

    public static Arm64Instruction Cbz(Arm64RegisterX register, Arm64LabelOffset label)
    {
        label.ValidateMultipleOf4();
        return Arm64InstructionFactory.CBZ(register, label).ToCapstoneArm64Instruction(); // cbz Xn, label
    }

    public static Arm64Instruction Cbnz(Arm64RegisterX register, Arm64LabelOffset label)
    {
        label.ValidateMultipleOf4();
        return Arm64InstructionFactory.CBNZ(register, label).ToCapstoneArm64Instruction(); // cbnz Xn, label
    }

    // Unconditional branch
    public static Arm64Instruction B(Arm64LabelOffset label)
    {
        label.ValidateMultipleOf4();
        return Arm64InstructionFactory.B(label).ToCapstoneArm64Instruction();
    }

    // Conditional branch
    // B.EQ: Equal (Z == 1).
    // B.NE: Not equal (Z == 0).
    // B.HS: Carry set/unsigned higher or same (C == 1).
    // B.LO: Carry clear/unsigned lower (C == 0).
    // B.MI: Minus/negative (N == 1).
    // B.PL: Plus/positive or zero (N == 0).
    // B.VS: Overflow (V == 1).
    // B.VC: No overflow (V == 0).
    // B.HI: Unsigned higher (C == 1 and Z == 0).
    // B.LS: Unsigned lower or same (C == 0 or Z == 1).
    // B.GE: Signed greater than or equal (N == V).
    // B.LT: Signed less than (N != V).
    // B.GT: Signed greater than (Z == 0 and N == V).
    // B.LE: Signed less than or equal (Z == 1 or N != V).
    // B.AL: Always (unconditional execution).
    // B.NV: (this condition is deprecated or not used).
    public static Arm64Instruction B(Arm64ConditionalKind conditionalKind, Arm64LabelOffset label)
    {
        label.ValidateMultipleOf4();
        return Arm64InstructionFactory.B(conditionalKind, label).ToCapstoneArm64Instruction(); // B.EQ label
    }

    // RET with X30(Link Register)
    public static Arm64Instruction Ret()
        => Arm64InstructionFactory.RET().ToCapstoneArm64Instruction();

    public static Arm64Instruction Ret(Arm64RegisterX register)
        => Arm64InstructionFactory.RET(register).ToCapstoneArm64Instruction();

    public static Arm64Instruction Nop()
        => Arm64InstructionFactory.NOP().ToCapstoneArm64Instruction();

    public static Arm64Instruction Tbz(Arm64RegisterX register, byte imm, Arm64LabelOffset label)
    {
        label.ValidateMultipleOf4();
        return Arm64InstructionFactory.TBZ(register, imm, label).ToCapstoneArm64Instruction();
    }

    public static Arm64Instruction Bl(Arm64LabelOffset label)
    {
        label.ValidateMultipleOf4();
        return Arm64InstructionFactory.BL(label).ToCapstoneArm64Instruction();
    }

    public static Arm64Instruction Blr(Arm64RegisterX register)
    {
        return Arm64InstructionFactory.BLR(register).ToCapstoneArm64Instruction();
    }

    public static Arm64Instruction Drps()
      => Arm64InstructionFactory.DRPS().ToCapstoneArm64Instruction();

    public static Arm64Instruction RetAA()
       => Arm64InstructionFactory.RETAA().ToCapstoneArm64Instruction();
}

internal static class ExtensionMethods
{
    public static AsmArm64.Arm64Instruction ToAsmArm64Instruction(this uint rawInstruction)
        => AsmArm64.Arm64Instruction.Decode(rawInstruction);

    public static Arm64Instruction ToCapstoneArm64Instruction(this uint rawInstruction, ulong baseAddress, uint i)
    {
        ulong offset = (ulong)i * 4;
        return rawInstruction.ToCapstoneArm64Instruction(baseAddress + offset);
    }

    public static Arm64Instruction ToCapstoneArm64Instruction(this uint rawInstruction, ulong baseAddress = 0)
    {
        Span<byte> bytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes, rawInstruction);

        using var disassembler = CapstoneDisassembler.CreateArm64Disassembler(Arm64DisassembleMode.Arm);
        disassembler.EnableInstructionDetails = true;

        var instructions = disassembler.Disassemble(bytes.ToArray(), (long)baseAddress);
        if (instructions.Length == 0)
        {
            var instruction = AsmArm64.Arm64Instruction.Decode(rawInstruction);
            throw new Exception($"CapstoneDisassembler failed to deserialize instruction: {instruction.ToString()} (rawInstruction: 0x{rawInstruction:X2})");
        }

        return instructions.First();
    }

    public static byte[] ToLittleEndianBytes(this uint[] rawInstructions)
    {
        if (BitConverter.IsLittleEndian)
            return MemoryMarshal.AsBytes(rawInstructions).ToArray();

        byte[] bytes = new byte[rawInstructions.Length * 4];

        for (int i = 0; i < rawInstructions.Length; i++)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(i * 4, 4), rawInstructions[i]);
        }
        return bytes;
    }

    public static void ValidateMultipleOf4(this Arm64LabelOffset label)
    {
        if (label.Value % 4 != 0)
            throw new ArgumentException("Arm64LabelOffset value must be multiple of 4");
    }

    public static void ValidateMultipleOf8(this Arm64LabelOffset label)
    {
        if (label.Value % 8 != 0)
            throw new ArgumentException("Arm64LabelOffset value must be multiple of 8");
    }
}
