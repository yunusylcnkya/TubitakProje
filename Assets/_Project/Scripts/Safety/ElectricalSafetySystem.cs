using System;
using UnityEngine;

public sealed class ElectricalSafetySystem :
    MonoBehaviour
{
    [Header("Systems")]
    [SerializeField]
    private ElectricalSimulationSystem
        electricalSimulation;

    [Header("Behaviour")]
    [SerializeField]
    private bool blockUnsafeActions = false;

    public bool BlockUnsafeActions =>
        blockUnsafeActions;

    public event Action<string>
        SafetyViolationRaised;

    public bool IsTerminalEnergized(
        Terminal terminal)
    {
        if (terminal == null)
            return false;

        if (electricalSimulation == null ||
            electricalSimulation.LastSnapshot == null)
        {
            return false;
        }

        ElectricalPotentialFlags state =
            electricalSimulation
                .LastSnapshot
                .GetPotential(terminal);

        return state !=
            ElectricalPotentialFlags.None;
    }

    public bool IsWireEnergized(
        Wire wire)
    {
        if (wire == null)
            return false;

        return
            IsTerminalEnergized(
                wire.StartTerminal
            )
            ||
            IsTerminalEnergized(
                wire.EndTerminal
            );
    }

    public bool AuthorizeWireRemoval(
        Wire wire)
    {
        if (wire == null)
            return false;

        if (!IsWireEnergized(wire))
            return true;

        string message =
            "GÜVENLİK UYARISI: " +
            "Enerjili kablo üzerinde işlem yapıldı.";

        Debug.LogWarning(
            message,
            wire
        );

        SafetyViolationRaised?.Invoke(
            message
        );

        return !blockUnsafeActions;
    }
}