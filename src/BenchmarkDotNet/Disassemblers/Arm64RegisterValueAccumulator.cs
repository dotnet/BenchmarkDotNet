using Gee.External.Capstone.Arm64;
using Microsoft.Diagnostics.Runtime.Interfaces;

namespace BenchmarkDotNet.Disassemblers;

internal struct Arm64RegisterValueAccumulator
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
    private IClrRuntime _runtime;

    public void Init(IClrRuntime runtime)
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
