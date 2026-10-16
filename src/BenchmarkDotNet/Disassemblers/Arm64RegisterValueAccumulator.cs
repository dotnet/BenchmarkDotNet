using Gee.External.Capstone.Arm64;
using Microsoft.Diagnostics.Runtime.Interfaces;
using System.Diagnostics;

namespace BenchmarkDotNet.Disassemblers;

internal struct Arm64RegisterValueAccumulator
{
    internal enum State
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
        _runtime = runtime;
        Reset();
    }

    internal void Reset(
        State state = State.LookingForPattern,
        int expectedMovkShift = 0,
        long value = 0,
        Arm64RegisterId registerId = Arm64RegisterId.Invalid)
    {
        _state = state;
        _expectedMovkShift = expectedMovkShift;
        _value = value;
        _registerId = registerId;
    }

    public void Feed(Arm64Instruction instruction)
    {
        switch (_state)
        {
            case State.LookingForPattern:
                if (TryHandleLookingForPattern(instruction))
                    return;
                break;

            case State.ExpectingMovk:
                if (TryHandleExpectingMovk(instruction))
                    return;

                _state = State.LookingForPossibleLdr;
                goto case State.LookingForPossibleLdr;

            case State.ExpectingAdd:
                if (TryHandleExpectingAdd(instruction))
                    return;
                break;

            case State.LookingForPossibleLdr:
                if (TryHandleLdrFromTrackedRegister(instruction))
                    return;

                if (TryHandleBranchOrRestart(instruction))
                    return;

                // Finally check if the current instruction modified the register that was accumulating the constant
                // and reset the state machine in case it did.
                foreach (Arm64Register reg in instruction.Details.AllWrittenRegisters)
                {
                    // Some unexpected instruction overwriting the accumulated register
                    if (reg.Id == _registerId)
                    {
                        _state = State.LookingForPattern;
                    }
                }
                break;
        }
    }

    public bool HasValue => _state == State.ExpectingMovk || _state == State.LookingForPossibleLdr;

    public long Value => _value;

    public Arm64RegisterId RegisterId => _registerId;

    private bool TryHandleLookingForPattern(Arm64Instruction instruction)
    {
        switch (instruction.Id)
        {
            case Arm64InstructionId.ARM64_INS_MOVZ:
                return TryStartMovzSequence(instruction);
            case Arm64InstructionId.ARM64_INS_ADRP:
                return TryStartAdrpSequence(instruction);
            default:
                return false;
        }
    }

    private bool TryStartMovzSequence(Arm64Instruction instruction)
    {
        try
        {
            if (!instruction.TryGetOperand(0, Arm64OperandType.Register, out _))
                return false;

            if (!instruction.TryGetOperand(1, Arm64OperandType.Immediate, out _))
                return false;

            _registerId = instruction.GetRegisterId(0);
            _value = instruction.GetRawImmediateValue(1);
            _state = State.ExpectingMovk;
            _expectedMovkShift = 16;
            return true;
        }
        catch (Exception ex) when (IsDecodingException(ex))
        {
            return false;
        }
    }

    private bool TryStartAdrpSequence(Arm64Instruction instruction)
    {
        try
        {
            if (!instruction.TryGetOperand(0, Arm64OperandType.Register, out _))
                return false;

            if (!instruction.TryGetOperand(1, Arm64OperandType.Immediate, out _))
                return false;

            _registerId = instruction.GetRegisterId(0);
            _value = instruction.GetRawImmediateValue(1);
            _state = State.ExpectingAdd;
            return true;
        }
        catch (Exception ex) when (IsDecodingException(ex))
        {
            return false;
        }
    }

    private bool TryHandleExpectingMovk(Arm64Instruction instruction)
    {
        try
        {
            if (instruction.Id != Arm64InstructionId.ARM64_INS_MOVK)
                return false;

            if (instruction.GetRegisterId(0) != _registerId)
                return false;

            Arm64InstructionDetail details = instruction.Details;
            if (details.Operands[1].ShiftOperation != Arm64ShiftOperation.ARM64_SFT_LSL
                || details.Operands[1].ShiftValue != _expectedMovkShift)
                return false;

            _value = _value | (instruction.GetRawImmediateValue(1) << details.Operands[1].ShiftValue);
            _expectedMovkShift += 16;
            return true;
        }
        catch (Exception ex) when (IsDecodingException(ex))
        {
            return false;
        }
    }

    private bool TryHandleExpectingAdd(Arm64Instruction instruction)
    {
        try
        {
            if (instruction.Id != Arm64InstructionId.ARM64_INS_ADD)
                return false;

            if (instruction.GetRegisterId(0) != _registerId)
                return false;

            if (instruction.GetRegisterId(1) != _registerId)
                return false;

            if (!instruction.TryGetOperand(2, Arm64OperandType.Immediate, out _))
                return false;

            _value = _value | instruction.GetRawImmediateValue(2);
            _state = State.LookingForPossibleLdr;
            return true;
        }
        catch (Exception ex) when (IsDecodingException(ex))
        {
            return false;
        }
    }

    private bool TryHandleLdrFromTrackedRegister(Arm64Instruction instruction)
    {
        try
        {
            if (!instruction.IsLdrFromTrackedRegister(_registerId))
                return false;

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
                _registerId = instruction.GetRegisterId(0);
            }

            return true;
        }
        catch (Exception ex) when (IsDecodingException(ex))
        {
            return false;
        }
    }

    private bool TryHandleBranchOrRestart(Arm64Instruction instruction)
    {
        Arm64InstructionDetail details = instruction.Details;

        if (instruction.Id == Arm64InstructionId.ARM64_INS_CBZ ||
            instruction.Id == Arm64InstructionId.ARM64_INS_CBNZ ||
            instruction.Id == Arm64InstructionId.ARM64_INS_B && details.ConditionCode != Arm64ConditionCode.Invalid)
        {
            // ignore conditional branches
            return true;
        }

        if (details.BelongsToGroup(Arm64InstructionGroupId.ARM64_GRP_BRANCH_RELATIVE) ||
            details.BelongsToGroup(Arm64InstructionGroupId.ARM64_GRP_CALL) ||
            details.BelongsToGroup(Arm64InstructionGroupId.ARM64_GRP_JUMP))
        {
            // We've encountered an unconditional jump or call, the accumulated registers value is not valid anymore
            _state = State.LookingForPattern;
            return true;
        }

        if (instruction.Id == Arm64InstructionId.ARM64_INS_MOVZ)
        {
            // Another constant loading is starting: restart matching from LookingForPattern.
            _state = State.LookingForPattern;
            TryStartMovzSequence(instruction);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Only decoding failures (missing/mistyped operands, unexpected shift/instruction)
    /// are treated as "not matched". Anything else (e.g. OOM) must propagate.
    /// </summary>
    private static bool IsDecodingException(Exception ex)
        => ex is ArgumentOutOfRangeException
            or ArgumentException
            or NotSupportedException
            or InvalidOperationException
            or UnreachableException;
}
