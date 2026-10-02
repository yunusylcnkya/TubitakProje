using System.Collections.Generic;
using UnityEngine;

public sealed class ThermalRelayDevice :
    ElectricalDeviceBase
{
    [Header("Power Path - L1")]
    [SerializeField]
    private Terminal phaseL1Input;

    [SerializeField]
    private Terminal phaseL1Output;

    [Header("Power Path - L2")]
    [SerializeField]
    private Terminal phaseL2Input;

    [SerializeField]
    private Terminal phaseL2Output;

    [Header("Power Path - L3")]
    [SerializeField]
    private Terminal phaseL3Input;

    [SerializeField]
    private Terminal phaseL3Output;

    [Header("Auxiliary NC Contact")]
    [SerializeField]
    private Terminal auxiliary95;

    [SerializeField]
    private Terminal auxiliary96;

    [Header("State")]
    [SerializeField]
    private bool isTripped;

    public bool IsTripped =>
        isTripped;

    public override void CollectActiveLinks(
        List<ElectricalLink> links)
    {
        if (!IsOperational)
            return;

        // Ana güç yolları fiziksel olarak
        // termik üzerinden devam eder.
        AddLink(
            links,
            phaseL1Input,
            phaseL1Output
        );

        AddLink(
            links,
            phaseL2Input,
            phaseL2Output
        );

        AddLink(
            links,
            phaseL3Input,
            phaseL3Output
        );

        // 95-96 yalnızca termik normal
        // durumdayken kapalıdır.
        if (!isTripped)
        {
            AddLink(
                links,
                auxiliary95,
                auxiliary96
            );
        }
    }

    private void AddLink(
        List<ElectricalLink> links,
        Terminal terminalA,
        Terminal terminalB)
    {
        ElectricalLink link =
            new ElectricalLink(
                terminalA,
                terminalB
            );

        if (link.IsValid)
        {
            links.Add(link);
        }
    }

    [ContextMenu("Trip Thermal Relay")]
    public void Trip()
    {
        if (isTripped)
            return;

        isTripped = true;

        Debug.Log(
            $"THERMAL RELAY | {name} | TRIPPED"
        );

        NotifyStateChanged();
    }

    [ContextMenu("Reset Thermal Relay")]
    public void ResetRelay()
    {
        if (!isTripped)
            return;

        isTripped = false;

        Debug.Log(
            $"THERMAL RELAY | {name} | RESET"
        );

        NotifyStateChanged();
    }
}