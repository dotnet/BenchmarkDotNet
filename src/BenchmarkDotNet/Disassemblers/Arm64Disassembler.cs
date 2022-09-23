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
            LookingForMovz,
            ExpectingMovk,
            LookingForPossibleLdr
        }

        private State _state;
        private long _value;
        private int _expectedMovkShift;
        private Arm64RegisterId _registerId;
        private ClrRuntime _runtime;

        public void Init(ClrRuntime runtime)
        {
            _state = State.LookingForMovz;
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
                case State.LookingForMovz:
                    if (instruction.Id == Arm64InstructionId.ARM64_INS_MOVZ)
                    {
                        _registerId = details.Operands[0].Register.Id;
                        _value = details.Operands[1].Immediate;
                        _state = State.ExpectingMovk;
                        _expectedMovkShift = 16;
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
                case State.LookingForPossibleLdr:
                    if (instruction.Id == Arm64InstructionId.ARM64_INS_LDR &&
                        _registerId == details.Operands[0].Register.Id && // Target of the LDR is the register we are tracking
                        details.Operands[1].Type == Arm64OperandType.Memory &&
                        details.Operands[1].Memory.Base.Id == _registerId && // The source address is in the register we are tracking
                        details.Operands[1].Memory.Displacement == 0 && // There is no displacement
                        details.Operands[1].Memory.Index == null) // And there is no extra index register
                    {
                        // Simulate the LDR instruction.
                        long newValue = (long)_runtime.DataTarget.DataReader.ReadPointer((ulong)_value);
                        //Console.WriteLine($"Reading from memory at {_value:X}, got {newValue:X}");
                        _value = newValue;
                        if (_value == 0)
                        {
                            _state = State.LookingForMovz;
                        }
                    }
                    else if (instruction.Id == Arm64InstructionId.ARM64_INS_CBZ ||
                            instruction.Id == Arm64InstructionId.ARM64_INS_CBNZ ||
                            instruction.Id == Arm64InstructionId.ARM64_INS_B && details.ConditionCode != Arm64ConditionCode.Invalid)
                    {
                        // ignore conditional branches
                        //Console.WriteLine($"Ignoring conditional branch {instruction.Id}");
                    }
                    else if (details.BelongsToGroup(Arm64InstructionGroupId.ARM64_GRP_BRANCH_RELATIVE) ||
                             details.BelongsToGroup(Arm64InstructionGroupId.ARM64_GRP_CALL) ||
                             details.BelongsToGroup(Arm64InstructionGroupId.ARM64_GRP_JUMP))
                    {
                        // We've encountered an unconditional jump or call, the accumulated registers value is not valid anymore
                        //Console.WriteLine($"Resetting state at branch");
                        _state = State.LookingForMovz;
                    }
                    else if (instruction.Id == Arm64InstructionId.ARM64_INS_MOVZ)
                    {
                        // Another constant loading is starting
                        _state = State.LookingForMovz;
                        goto case State.LookingForMovz;
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
                                //Console.WriteLine($"Resetting state at register writing");
                                _state = State.LookingForMovz;
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

    internal class Arm64Disassembler : ClrMdV2Disassembler<Arm64Instruction>
    {
        protected override IEnumerable<Asm> Decode(byte[] code, ulong startAddress, State state, int depth, ClrMethod currentMethod)
        {
            const Arm64DisassembleMode disassembleMode = Arm64DisassembleMode.Arm;
            using (CapstoneArm64Disassembler disassembler = CapstoneDisassembler.CreateArm64Disassembler(disassembleMode))
            {
                // Enables disassemble details, which are disabled by default, to provide more detailed information on
                // disassembled binary code.
                disassembler.EnableInstructionDetails = true;
                disassembler.DisassembleSyntax = DisassembleSyntax.Intel;
                RegisterValueAccumulator accumulator = new RegisterValueAccumulator();
                accumulator.Init(state.Runtime);

                Arm64Instruction[] instructions = disassembler.Disassemble(code, (long)startAddress);
                foreach (Arm64Instruction instruction in instructions)
                {
                    // TODO: use the accumulated address
                    // TODO: set the isIndirect correctly
                    bool isIndirect = false;
                    ulong address = 0;
                    if (TryGetReferencedAddress(instruction, accumulator, (uint)state.Runtime.DataTarget.DataReader.PointerSize, out address, out isIndirect))
                    {
                        TryTranslateAddressToName(address, isAddressPrecodeMD: false, state, isIndirect, depth, currentMethod);
                    }

                    accumulator.Feed(instruction);

                    yield return new Asm()
                    {
                        InstructionPointer = (ulong)instruction.Address,
                        InstructionLength = instruction.Bytes.Length,
                        Arm64Instruction = instruction,
                        ReferencedAddress = (address > ushort.MaxValue) ? address : null,
                        IsReferencedAddressIndirect = isIndirect,
                        AddressToNameMapping = state.AddressToNameMapping
                    };
                }
            }
        }

        internal static bool TryGetReferencedAddress(Arm64Instruction instruction, RegisterValueAccumulator accumulator, uint pointerSize, out ulong referencedAddress, out bool isReferencedAddressIndirect)
        {
            if ((instruction.Id == Arm64InstructionId.ARM64_INS_BR || instruction.Id == Arm64InstructionId.ARM64_INS_BLR) && instruction.Details.Operands[0].Register.Id == accumulator.RegisterId && accumulator.HasValue)
            {
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
    }
}
