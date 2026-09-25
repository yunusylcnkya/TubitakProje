using System;
using UnityEngine;

[Serializable]
public sealed class TaskStepDefinition
{
    [Header("Identity")]
    [SerializeField]
    private string stepId;

    [TextArea]
    [SerializeField]
    private string instruction;

    [Header("Condition")]
    [SerializeField]
    private TaskStepType stepType;

    [Header("Equipment Placement")]
    [SerializeField]
    private string equipmentId;

    [SerializeField]
    private string slotId;

    [Header("Connection")]
    [SerializeField]
    private string terminalIdA;

    [SerializeField]
    private string terminalIdB;

    public string StepId =>
        stepId;

    public string Instruction =>
        instruction;

    public TaskStepType StepType =>
        stepType;

    public string EquipmentId =>
        equipmentId;

    public string SlotId =>
        slotId;

    public string TerminalIdA =>
        terminalIdA;

    public string TerminalIdB =>
        terminalIdB;
}