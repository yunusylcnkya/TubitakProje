using System;
using UnityEngine;

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
}
