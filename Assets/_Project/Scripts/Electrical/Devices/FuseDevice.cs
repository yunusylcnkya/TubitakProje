using System.Collections.Generic;
using UnityEngine;

public sealed class FuseDevice :
    ElectricalDeviceBase
{
    [Header("Terminals")]
    [SerializeField]
    private Terminal inputTerminal;

    [SerializeField]
    private Terminal outputTerminal;

    [Header("Protection")]
    [SerializeField]
    private bool autoTripOnDCShortCircuit = true;

    [Header("State")]
    [SerializeField]
    private bool isBlown;

    private string lastTripReason;

    public bool IsBlown =>
        isBlown;

    public string LastTripReason =>
        lastTripReason;

    public override void CollectActiveLinks(
        List<ElectricalLink> links)
    {
        if (!IsOperational)
            return;

        if (isBlown)
            return;

        ElectricalLink link =
            new ElectricalLink(
                inputTerminal,
                outputTerminal
            );

        if (link.IsValid)
        {
            links.Add(link);
        }
    }

    public override bool EvaluateState(
        ElectricalSimulationSnapshot snapshot)
    {
        if (!IsOperational)
            return false;

        if (isBlown)
            return false;

        if (!autoTripOnDCShortCircuit)
            return false;

        if (snapshot == null)
            return false;

        bool hasPositive =
            snapshot.HasPotential(
                outputTerminal,
                ElectricalPotentialFlags.DCPositive
            );

        bool hasNegative =
            snapshot.HasPotential(
                outputTerminal,
                ElectricalPotentialFlags.DCNegative
            );

        if (!hasPositive ||
            !hasNegative)
        {
            return false;
        }

        TripInternal(
            "DC_SHORT_CIRCUIT"
        );

        return true;
    }

    [ContextMenu("Blow Fuse")]
    public void Blow()
    {
        if (isBlown)
            return;

        TripInternal(
            "MANUAL"
        );
    }

    private void TripInternal(
        string reason)
    {
        if (isBlown)
            return;

        isBlown = true;
        lastTripReason = reason;

        Debug.LogWarning(
            $"FUSE | {name} | BLOWN | {reason}",
            this
        );

        NotifyStateChanged();
    }

    [ContextMenu("Reset Fuse")]
    public void ResetFuse()
    {
        if (!isBlown)
            return;

        isBlown = false;
        lastTripReason = string.Empty;

        Debug.Log(
            $"FUSE | {name} | RESET",
            this
        );

        NotifyStateChanged();
    }
}