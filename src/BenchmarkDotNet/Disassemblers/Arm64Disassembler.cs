using BenchmarkDotNet.Diagnosers;
using Gee.External.Capstone;
using Gee.External.Capstone.Arm64;
using Microsoft.Diagnostics.Runtime;

namespace BenchmarkDotNet.Disassemblers
{
    internal struct RegisterValueAccumulator
    {
        private enum State
        {
            LookingForPattern,
            ExpectingMovk,
            ExpectingAdd,
            LookingForPossibleLdr
        }

        private State _state;
        private long _value;
        private int _expectedMovkShift;
        private Arm64RegisterId _registerId;
        private ClrRuntime _runtime;

        public void Init(ClrRuntime runtime)
        {
            _state = State.LookingForPattern;
            _expectedMovkShift = 0;
            _value = 0;
            _registerId = Arm64RegisterId.Invalid;
            _runtime = runtime;
        }

        public void Feed(Arm64Instruction instruction)
        {
            Arm64InstructionDetail details = instruction.Details;

            switch (_state)
            {
                case State.LookingForPattern:
                    if (instruction.Id == Arm64InstructionId.ARM64_INS_MOVZ)
                    {
                        _registerId = details.Operands[0].Register.Id;
                        _value = details.Operands[1].Immediate;
                        _state = State.ExpectingMovk;
                        _expectedMovkShift = 16;
                    }
                    else if (instruction.Id == Arm64InstructionId.ARM64_INS_ADRP)
                    {
                        _registerId = details.Operands[0].Register.Id;
                        _value = details.Operands[1].Immediate;
                        _state = State.ExpectingAdd;
                    }
                    break;
                case State.ExpectingMovk:
                    if (instruction.Id == Arm64InstructionId.ARM64_INS_MOVK &&
                        details.Operands[0].Register.Id == _registerId &&
                        details.Operands[1].ShiftOperation == Arm64ShiftOperation.ARM64_SFT_LSL &&
                        details.Operands[1].ShiftValue == _expectedMovkShift)
                    {
                        _value = _value | (instruction.Details.Operands[1].Immediate << details.Operands[1].ShiftValue);
                        _expectedMovkShift += 16;
                        break;
                    }
                    _state = State.LookingForPossibleLdr;
                    goto case State.LookingForPossibleLdr;
                case State.ExpectingAdd:
                    if (instruction.Id == Arm64InstructionId.ARM64_INS_ADD &&
                        details.Operands[0].Register.Id == _registerId &&
                        details.Operands[1].Register.Id == _registerId &&
                        details.Operands[2].Type == Arm64OperandType.Immediate)
                    {
                        _value = _value | instruction.Details.Operands[2].Immediate;
                        _state = State.LookingForPossibleLdr;
                    }
                    break;
                case State.LookingForPossibleLdr:
                    if (instruction.Id == Arm64InstructionId.ARM64_INS_LDR &&
                        details.Operands[1].Type == Arm64OperandType.Memory &&
                        details.Operands[1].Memory.Base.Id == _registerId && // The source address is in the register we are tracking
                        details.Operands[1].Memory.Displacement == 0 && // There is no displacement
                        details.Operands[1].Memory.Index == null) // And there is no extra index register
                    {
                        // Simulate the LDR instruction.
                        long newValue = (long)_runtime.DataTarget.DataReader.ReadPointer((ulong)_value);
                        _value = newValue;
                        if (_value == 0)
                        {
                            _state = State.LookingForPattern;
                        }
                        else
                        {
                            // The LDR might have loaded the result in another register
                            _registerId = details.Operands[0].Register.Id;
                        }
                    }
                    else if (instruction.Id == Arm64InstructionId.ARM64_INS_CBZ ||
                            instruction.Id == Arm64InstructionId.ARM64_INS_CBNZ ||
                            instruction.Id == Arm64InstructionId.ARM64_INS_B && details.ConditionCode != Arm64ConditionCode.Invalid)
                    {
                        // ignore conditional branches
                    }
                    else if (details.BelongsToGroup(Arm64InstructionGroupId.ARM64_GRP_BRANCH_RELATIVE) ||
                             details.BelongsToGroup(Arm64InstructionGroupId.ARM64_GRP_CALL) ||
                             details.BelongsToGroup(Arm64InstructionGroupId.ARM64_GRP_JUMP))
                    {
                        // We've encountered an unconditional jump or call, the accumulated registers value is not valid anymore
                        _state = State.LookingForPattern;
                    }
                    else if (instruction.Id == Arm64InstructionId.ARM64_INS_MOVZ)
                    {
                        // Another constant loading is starting
                        _state = State.LookingForPattern;
                        goto case State.LookingForPattern;
                    }
                    else
                    {
                        // Finally check if the current instruction modified the register that was accumulating the constant
                        // and reset the state machine in case it did.
                        foreach (Arm64Register reg in details.AllWrittenRegisters)
                        {
                            // Some unexpected instruction overwriting the accumulated register
                            if (reg.Id == _registerId)
                            {
                                _state = State.LookingForPattern;
                            }
                        }
                    }
                    break;
            }
        }

        public bool HasValue => _state == State.ExpectingMovk || _state == State.LookingForPossibleLdr;

        public long Value { get { return _value; } }

        public Arm64RegisterId RegisterId { get { return _registerId; } }
    }

    internal class Arm64Disassembler : ClrMdDisassembler
    {
        internal sealed class RuntimeSpecificData
        {
            // See dotnet/runtime src/coreclr/vm/arm64/thunktemplates.asm/.S for the stub code
            // ldr  x9, DATA_SLOT(CallCountingStub, RemainingCallCountCell)
            // ldrh w10, [x9]
            // subs w10, w10, #0x1
            internal readonly byte[] callCountingStubTemplate = [0x09, 0x00, 0x00, 0x58, 0x2a, 0x01, 0x40, 0x79, 0x4a, 0x05, 0x00, 0x71];
            // ldr x10, DATA_SLOT(StubPrecode, Target)
            // ldr x12, DATA_SLOT(StubPrecode, MethodDesc)
            // br x10
            internal readonly byte[] stubPrecodeTemplate = [0x4a, 0x00, 0x00, 0x58, 0xec, 0x00, 0x00, 0x58, 0x40, 0x01, 0x1f, 0xd6];
            // ldr x11, DATA_SLOT(FixupPrecode, Target)
            // br  x11
            // ldr x12, DATA_SLOT(FixupPrecode, MethodDesc)
            internal readonly byte[] fixupPrecodeTemplate = [0x0b, 0x00, 0x00, 0x58, 0x60, 0x01, 0x1f, 0xd6, 0x0c, 0x00, 0x00, 0x58];
            internal readonly ulong stubPageSize;

            internal RuntimeSpecificData(State state)
            {
                stubPageSize = (ulong)Environment.SystemPageSize;
                if (state.RuntimeVersion.Major >= 8)
                {
                    // In .NET 8, the stub page size was changed to min 16kB
                    stubPageSize = Math.Max(stubPageSize, 16384);
                }

                // The stubs code depends on the current OS memory page size, so we need to update the templates to reflect that
                ulong pageSizeShifted = stubPageSize / 32;
                // Calculate the ldr x9, #offset instruction with offset based on the page size
                callCountingStubTemplate[1] = (byte)(pageSizeShifted & 0xff);
                callCountingStubTemplate[2] = (byte)(pageSizeShifted >> 8);

                // Calculate the ldr x10, #offset instruction with offset based on the page size
                stubPrecodeTemplate[1] = (byte)(pageSizeShifted & 0xff);
                stubPrecodeTemplate[2] = (byte)(pageSizeShifted >> 8);
                // Calculate the ldr x12, #offset instruction with offset based on the page size
                stubPrecodeTemplate[5] = (byte)((pageSizeShifted - 1) & 0xff);
                stubPrecodeTemplate[6] = (byte)((pageSizeShifted - 1) >> 8);

                // Calculate the ldr x11, #offset instruction with offset based on the page size
                fixupPrecodeTemplate[1] = (byte)(pageSizeShifted & 0xff);
                fixupPrecodeTemplate[2] = (byte)(pageSizeShifted >> 8);
                // Calculate the ldr x12, #offset instruction with offset based on the page size
                fixupPrecodeTemplate[9] = (byte)(pageSizeShifted & 0xff);
                fixupPrecodeTemplate[10] = (byte)(pageSizeShifted >> 8);
            }
        }

        private static readonly Dictionary<Version, RuntimeSpecificData> runtimeSpecificData = [];

        protected override IEnumerable<Asm> Decode(byte[] code, ulong startAddress, State state, int depth, ClrMethod currentMethod, DisassemblySyntax syntax)
        {
            if (!runtimeSpecificData.TryGetValue(state.RuntimeVersion, out var data))
            {
                runtimeSpecificData.Add(state.RuntimeVersion, data = new RuntimeSpecificData(state));
            }

            const Arm64DisassembleMode disassembleMode = Arm64DisassembleMode.Arm;
            using (CapstoneArm64Disassembler disassembler = CapstoneDisassembler.CreateArm64Disassembler(disassembleMode))
            {
                // Enables disassemble details, which are disabled by default, to provide more detailed information on
                // disassembled binary code.
                disassembler.EnableInstructionDetails = true;
                disassembler.DisassembleSyntax = Map(syntax);
                RegisterValueAccumulator accumulator = new RegisterValueAccumulator();
                accumulator.Init(state.Runtime);

                Arm64Instruction[] instructions = disassembler.Disassemble(code, (long)startAddress);
                foreach (Arm64Instruction instruction in instructions)
                {
                    bool isIndirect = false;
                    bool isPrestubMD = false;

                    ulong address = 0;
                    if (TryGetReferencedAddress(instruction, accumulator, (uint)state.Runtime.DataTarget.DataReader.PointerSize, out address, out isIndirect))
                    {
                        if (isIndirect && state.RuntimeVersion.Major >= 7)
                        {
                            FlushCachedDataIfNeeded(state.Runtime.DataTarget.DataReader, address, new byte[1]);
                            TryResolvePrecode(state.Runtime.DataTarget.DataReader, ref address, out isPrestubMD);
                        }
                        TryTranslateAddressToName(address, isPrestubMD, state, depth, currentMethod);
                    }

                    accumulator.Feed(instruction);

                    yield return new Arm64Asm()
                    {
                        InstructionPointer = (ulong)instruction.Address,
                        InstructionLength = instruction.Bytes.Length,
                        Instruction = instruction,
                        ReferencedAddress = (address > ushort.MaxValue) ? address : null,
                        IsReferencedAddressIndirect = isIndirect,
                        DisassembleSyntax = disassembler.DisassembleSyntax
                    };
                }
            }
        }

        // Counterpart of IntelDisassembler.TryResolvePrecode: recognise the AArch64 precode/stub
        // shapes by matching the fixed opcode bits and reading slot displacements out of the
        // encoded LDR-literal instructions, so the resolution survives StubPrecodeData layout
        // changes between runtime versions. Resolves to the MethodDesc handle when one is present
        // (so GetMethodByHandle can recover the live ClrMethod even if the call site is still
        // pointing at PreStub), and to the TargetForMethod slot for call-counting stubs.
        private static bool TryResolvePrecode(IDataReader reader, ref ulong address, out bool isPrestubMD)
        {
            isPrestubMD = false;
            byte[] buffer = new byte[12];
            if (reader.Read(address, buffer) != 12)
                return false;

            uint instr0 = ReadInstr(buffer, 0);
            uint instr1 = ReadInstr(buffer, 4);
            uint instr2 = ReadInstr(buffer, 8);

            // StubPrecode:
            //   LDR x10, Target         (LDR-literal, Rt=10)
            //   LDR x12, MethodDesc     (LDR-literal, Rt=12)
            //   BR  x10                 (0xD61F0140)
            if (IsLdrLiteral64(instr0, out int rt0, out int off0) && rt0 == 10
                && IsLdrLiteral64(instr1, out int rt1, out int off1) && rt1 == 12
                && instr2 == 0xD61F0140u)
            {
                ulong mdSlot = unchecked(address + 4 + (ulong)(long)off1);
                if (reader.ReadPointer(mdSlot, out ulong md) && IsValidAddress(md))
                {
                    address = md;
                    isPrestubMD = true;
                    return true;
                }
                return false;
            }

            // FixupPrecode:
            //   LDR x11, Target         (LDR-literal, Rt=11)
            //   BR  x11                 (0xD61F0160)
            //   LDR x12, MethodDesc     (LDR-literal, Rt=12)
            if (IsLdrLiteral64(instr0, out int rtA, out int _) && rtA == 11
                && instr1 == 0xD61F0160u
                && IsLdrLiteral64(instr2, out int rtB, out int off2) && rtB == 12)
            {
                ulong mdSlot = unchecked(address + 8 + (ulong)(long)off2);
                if (reader.ReadPointer(mdSlot, out ulong md) && IsValidAddress(md))
                {
                    address = md;
                    isPrestubMD = true;
                    return true;
                }
                return false;
            }

            // CallCountingStub:
            //   LDR  x9, RemainingCallCount   (LDR-literal, Rt=9)
            //   LDRH w10, [x9]                (0x79400140)
            //   SUBS w10, w10, #1             (0x7100054A)
            //   ... (TargetForMethod lives 8 bytes after RemainingCallCount in the data section)
            if (IsLdrLiteral64(instr0, out int rtC, out int offC) && rtC == 9
                && instr1 == 0x79400129u
                && instr2 == 0x7100054Au)
            {
                ulong countSlot = unchecked(address + (ulong)(long)offC);
                if (reader.ReadPointer(countSlot + 8, out ulong target) && IsValidAddress(target))
                {
                    address = target;
                    return true;
                }
                return false;
            }

            return false;
        }

        private static uint ReadInstr(byte[] buffer, int offset)
            => (uint)buffer[offset]
             | ((uint)buffer[offset + 1] << 8)
             | ((uint)buffer[offset + 2] << 16)
             | ((uint)buffer[offset + 3] << 24);

        // LDR (literal), 64-bit form. Encoding: bits[31:24]=0x58, bits[23:5]=imm19 (signed,
        // word-scaled offset relative to the LDR's own PC), bits[4:0]=Xt. Returns the destination
        // register and the byte-scaled offset from the LDR instruction's address to the loaded slot.
        private static bool IsLdrLiteral64(uint instr, out int rt, out int offsetBytes)
        {
            rt = 0;
            offsetBytes = 0;
            if ((instr & 0xFF000000u) != 0x58000000u)
                return false;
            rt = (int)(instr & 0x1Fu);
            int imm19 = (int)((instr >> 5) & 0x7FFFFu);
            // Sign-extend 19-bit imm to 32-bit.
            if ((imm19 & 0x40000) != 0)
                imm19 |= unchecked((int)0xFFF80000u);
            offsetBytes = imm19 * 4;
            return true;
        }

        private static bool TryGetReferencedAddress(Arm64Instruction instruction, RegisterValueAccumulator accumulator, uint pointerSize, out ulong referencedAddress, out bool isReferencedAddressIndirect)
        {
            if ((instruction.Id == Arm64InstructionId.ARM64_INS_BR || instruction.Id == Arm64InstructionId.ARM64_INS_BLR) && instruction.Details.Operands[0].Register.Id == accumulator.RegisterId && accumulator.HasValue)
            {
                // Branch via register where we have extracted the value of the register by parsing the disassembly
                referencedAddress = (ulong)accumulator.Value;
                isReferencedAddressIndirect = true;
                return true;
            }
            else if (instruction.Details.BelongsToGroup(Arm64InstructionGroupId.ARM64_GRP_BRANCH_RELATIVE))
            {
                // One of the operands is the address
                for (int i = 0; i < instruction.Details.Operands.Length; i++)
                {
                    if (instruction.Details.Operands[i].Type == Arm64OperandType.Immediate)
                    {
                        referencedAddress = (ulong)instruction.Details.Operands[i].Immediate;
                        isReferencedAddressIndirect = false;
                        return true;
                    }
                }
            }
            referencedAddress = 0;
            isReferencedAddressIndirect = false;
            return false;
        }

        private static DisassembleSyntax Map(DisassemblySyntax syntax)
            => syntax switch
            {
                DisassemblySyntax.Att => DisassembleSyntax.Att,
                DisassemblySyntax.Intel => DisassembleSyntax.Intel,
                _ => DisassembleSyntax.Masm
            };

        // Recognise the AArch64 jump trampoline shape the CLR JIT emits when a call's real target
        // is out of rel26 range (±128 MB), plus the precode/stub shapes the runtime emits as the
        // stable entry point for tiered methods (so a direct `BL imm26` landing on the precode
        // still resolves to the underlying method):
        //   B imm26   (bits[31:26] = 0b000101)   — target = address + sign_extended(imm26) * 4
        //   CallCountingStub  (template match)   — reads TargetForMethod slot at data offset 8
        //   StubPrecode       (template match)   — reads Target slot at data offset 8
        //   FixupPrecode      (template match)   — reads Target slot at data offset 0
        // Writes the resolved target into `target` and returns true if one matches. Wider register
        // -tracking patterns (ADRP/ADR + ADD + BR Xn, ADRP + LDR + BR Xn, etc.) outside the precode
        // templates aren't decoded here — they need the disassembler's register accumulator.
        protected override bool TryFollowJumpTrampoline(State state, ulong address, out ulong target)
        {
            target = 0;
            IDataReader dataReader = state.Runtime.DataTarget.DataReader;
            byte[] buffer = new byte[12];
            int read = dataReader.Read(address, buffer);
            if (read < 4)
                return false;

            uint instr = (uint)buffer[0] | ((uint)buffer[1] << 8) | ((uint)buffer[2] << 16) | ((uint)buffer[3] << 24);

            // B imm26 — bits[31:26] == 0b000101 (0x5)
            if ((instr >> 26) == 0x5)
            {
                uint imm26 = instr & 0x03FFFFFFu;
                // Sign-extend the 26-bit immediate to 32 bits, then multiply by 4 (instructions are 4-byte aligned).
                int offset = (int)(imm26 & 0x02000000u) != 0
                    ? unchecked((int)(imm26 | 0xFC000000u)) << 2
                    : (int)imm26 << 2;
                target = unchecked(address + (ulong)(long)offset);
                return IsValidAddress(target);
            }

            // Precode templates — all 12 bytes, all start with LDR (so the B-imm26 check above
            // doesn't catch them). Match the byte template and read the Target slot from the data
            // page one stub-page above the code page. Only the runtime-7+ thunks use this fixed
            // layout; older runtimes never get here.
            if (read >= 12 && state.RuntimeVersion.Major >= 7
                && runtimeSpecificData.TryGetValue(state.RuntimeVersion, out var data))
            {
                var span = new ReadOnlySpan<byte>(buffer, 0, 12);

                // CallCountingStub: TargetForMethod at data offset 8
                if (span.SequenceEqual(data.callCountingStubTemplate))
                {
                    const ulong TargetForMethodOffset = 8;
                    if (dataReader.ReadPointer(address + data.stubPageSize + TargetForMethodOffset, out target) && IsValidAddress(target))
                        return true;
                    target = 0;
                    return false;
                }

                // StubPrecode: Target at data offset 8 (MethodDesc at 0)
                if (span.SequenceEqual(data.stubPrecodeTemplate))
                {
                    const ulong TargetOffset = 8;
                    if (dataReader.ReadPointer(address + data.stubPageSize + TargetOffset, out target) && IsValidAddress(target))
                        return true;
                    target = 0;
                    return false;
                }

                // FixupPrecode: Target at data offset 0 (MethodDesc at 8)
                if (span.SequenceEqual(data.fixupPrecodeTemplate))
                {
                    const ulong TargetOffset = 0;
                    if (dataReader.ReadPointer(address + data.stubPageSize + TargetOffset, out target) && IsValidAddress(target))
                        return true;
                    target = 0;
                    return false;
                }
            }

            return false;
        }
    }
}
