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
    private ulong _addressValue;
    private long _addressOffset;
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
        ulong value = 0,
        Arm64RegisterId registerId = Arm64RegisterId.Invalid)
    {
        _state = state;
        _expectedMovkShift = expectedMovkShift;
        _addressValue = value;
        _addressOffset = 0;
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

    public bool HasValue
    {
        get
        {
            // Value == 0 means invalid (negative combined clamps to 0, LDR resets on 0),
            // So it is excluded to avoid a useless address lookup in the disassembler.
            if (Value == 0)
                return false;

            return _state == State.ExpectingMovk || _state == State.LookingForPossibleLdr;
        }
    }

    /// <summary>
    /// Gets accumulated address value.
    /// Address arithmetic wraps modulo 2^64, like hardware.
    /// A negative combined value is invalid as a real address, so 0 is returned
    /// (0 is already treated as invalid: LDR resets on 0, disassembler filters it).
    /// </summary>
    public ulong Value
    {
        get
        {
            if (_addressOffset < 0)
            {
                // Magnitude of the negative offset, safe even for long.MinValue.
                ulong magnitude = unchecked((ulong)(-(_addressOffset + 1)) + 1);
                if (_addressValue < magnitude)
                    return 0;
            }
            return unchecked(_addressValue + (ulong)_addressOffset);
        }
    }

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
            _addressValue = (ulong)instruction.GetRawImmediateValue(1);
            _addressOffset = 0;
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
            _addressOffset = instruction.GetShiftedImmediateValue(1);
            _addressValue = 0;
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

            _addressValue = _addressValue | (ulong)instruction.GetRawImmediateValue(1) << details.Operands[1].ShiftValue;
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

            _addressValue = _addressValue | (ulong)instruction.GetRawImmediateValue(2);
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
            // Use combined Value: after ADRP+ADD the effective address is _addressValue + _addressOffset.
            // Use the Try-pattern so an unreadable address drops tracking instead of aborting disassembly.
            if (Value == 0)
            {
                _state = State.LookingForPattern;
                return true;
            }

            if (!_runtime.DataTarget.DataReader.ReadPointer(Value, out ulong dereferenced))
            {
                _state = State.LookingForPattern;
                return true;
            }

            _addressValue = dereferenced;
            _addressOffset = 0;
            if (_addressValue == 0)
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
