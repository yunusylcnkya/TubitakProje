using System;
using UnityEngine;

// Existing numeric values must remain unchanged: Unity serializes enum values.
public enum ExpectedMotorState
{
    Ignore = 0,
    Stopped = 1,
    Forward = 2,
    Reverse = 3,
    Invalid = 4
}

// Added for Training 07. Keep all numeric values stable.
public enum ExpectedTimeRelayState
{
    Ignore = 0,
    Idle = 1,
    Timing = 2,
    Elapsed = 3
}

[Serializable]
public sealed class TaskStepDefinition
{
    [Header("Identity")]
    [SerializeField] private string stepId;

    [TextArea]
    [SerializeField] private string instruction;

    [Header("Condition")]
    [SerializeField] private TaskStepType stepType;

    [Header("Equipment Placement")]
    [SerializeField] private EquipmentDefinition equipment;
    [SerializeField] private string slotId;

    [Header("Connection")]
    [SerializeField] private string terminalIdA;
    [SerializeField] private string terminalIdB;

    [Header("Functional State (only for FunctionalState steps)")]
    [SerializeField] private ExpectedSwitchState powerSupplyState = ExpectedSwitchState.Ignore;
    [SerializeField] private ExpectedSwitchState fuseState = ExpectedSwitchState.Ignore;
    [SerializeField] private ExpectedSwitchState lampState = ExpectedSwitchState.Ignore;

    [Header("START / Contactor (Temrin 02+, optional)")]
    [SerializeField] private ExpectedSwitchState startButtonState = ExpectedSwitchState.Ignore;
    [SerializeField] private ExpectedSwitchState stopButtonState = ExpectedSwitchState.Ignore;
    [SerializeField] private ExpectedSwitchState contactorState = ExpectedSwitchState.Ignore;

    [Header("3-phase breaker, thermal and motor (Temrin 04+, optional)")]
    [SerializeField] private ExpectedSwitchState breakerState = ExpectedSwitchState.Ignore;
    [SerializeField] private ExpectedSwitchState thermalTripState = ExpectedSwitchState.Ignore;
    [SerializeField] private ExpectedMotorState motorState = ExpectedMotorState.Ignore;

    [Header("TON Time Relay (Temrin 07+, optional)")]
    [SerializeField] private ExpectedTimeRelayState timeRelayState = ExpectedTimeRelayState.Ignore;

    public string StepId => stepId;
    public string Instruction => instruction;
    public TaskStepType StepType => stepType;
    public EquipmentDefinition Equipment => equipment;
    public string SlotId => slotId?.Trim();
    public string TerminalIdA => terminalIdA?.Trim();
    public string TerminalIdB => terminalIdB?.Trim();
    public ExpectedSwitchState PowerSupplyState => powerSupplyState;
    public ExpectedSwitchState FuseState => fuseState;
    public ExpectedSwitchState LampState => lampState;
    public ExpectedSwitchState StartButtonState => startButtonState;
    public ExpectedSwitchState StopButtonState => stopButtonState;
    public ExpectedSwitchState ContactorState => contactorState;
    public ExpectedSwitchState BreakerState => breakerState;
    public ExpectedSwitchState ThermalTripState => thermalTripState;
    public ExpectedMotorState MotorState => motorState;
    public ExpectedTimeRelayState TimeRelayState => timeRelayState;
}
