using BenchmarkDotNet.Diagnosers;
using Gee.External.Capstone;
using Gee.External.Capstone.Arm64;
using Microsoft.Diagnostics.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;

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

        public Arm64RegisterId RegisterId {  get {  return _registerId; } }
    }

    internal class Arm64Disassembler : ClrMdV2Disassembler
    {
        internal sealed class RuntimeSpecificData
        {
            // See dotnet/runtime src/coreclr/vm/arm64/thunktemplates.asm/.S for the stub code
            // ldr  x9, DATA_SLOT(CallCountingStub, RemainingCallCountCell)
            // ldrh w10, [x9]
            // subs w10, w10, #0x1
            internal readonly byte[] callCountingStubTemplate = new byte[12] { 0x09, 0x00, 0x00, 0x58, 0x2a, 0x01, 0x40, 0x79, 0x4a, 0x05, 0x00, 0x71 };
            // ldr x10, DATA_SLOT(StubPrecode, Target)
            // ldr x12, DATA_SLOT(StubPrecode, MethodDesc)
            // br x10
            internal readonly byte[] stubPrecodeTemplate = new byte[12] { 0x4a, 0x00, 0x00, 0x58, 0xec, 0x00, 0x00, 0x58, 0x40, 0x01, 0x1f, 0xd6 };
            // ldr x11, DATA_SLOT(FixupPrecode, Target)
            // br  x11
            // ldr x12, DATA_SLOT(FixupPrecode, MethodDesc)
            internal readonly byte[] fixupPrecodeTemplate = new byte[12] { 0x0b, 0x00, 0x00, 0x58, 0x60, 0x01, 0x1f, 0xd6, 0x0c, 0x00, 0x00, 0x58 };
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

        private static readonly Dictionary<Version, RuntimeSpecificData> runtimeSpecificData = new ();

        protected override IEnumerable<Asm> Decode(byte[] code, ulong startAddress, State state, int depth, ClrMethod currentMethod, DisassemblySyntax syntax)
        {
            if (!runtimeSpecificData.TryGetValue(state.RuntimeVersion, out RuntimeSpecificData data))
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
                            // Check if the target is a known stub
                            // The stubs are allocated in interleaved code / data pages in memory. The data part of the stub
                            // is at an address one memory page higher than the code.
                            byte[] buffer = new byte[12];

                            FlushCachedDataIfNeeded(state.Runtime.DataTarget.DataReader, address, buffer);

                            if (state.Runtime.DataTarget.DataReader.Read(address, buffer) == buffer.Length)
                            {
                                if (buffer.SequenceEqual(data.callCountingStubTemplate))
                                {
                                    const ulong TargetMethodAddressSlotOffset = 8;
                                    address = state.Runtime.DataTarget.DataReader.ReadPointer(address + data.stubPageSize + TargetMethodAddressSlotOffset);
                                }
                                else if (buffer.SequenceEqual(data.stubPrecodeTemplate))
                                {
                                    const ulong MethodDescSlotOffset = 0;
                                    address = state.Runtime.DataTarget.DataReader.ReadPointer(address + data.stubPageSize + MethodDescSlotOffset);
                                    isPrestubMD = true;
                                }
                                else if (buffer.SequenceEqual(data.fixupPrecodeTemplate))
                                {
                                    const ulong MethodDescSlotOffset = 8;
                                    address = state.Runtime.DataTarget.DataReader.ReadPointer(address + data.stubPageSize + MethodDescSlotOffset);
                                    isPrestubMD = true;
                                }
                            }
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
                        IsReferencedAddressIndirect = isIndirect
                    };
                }
            }
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
    }
}
