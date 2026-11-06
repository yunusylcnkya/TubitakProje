using System.Collections.Generic;
using UnityEngine;

// Educational TEST/RESET model. This is NOT a current/temperature overload simulator.
public sealed class ThermalRelayDevice : ElectricalDeviceBase, IPlacedEquipmentControl
{
    [Header("Power Path - L1")]
    [SerializeField] private Terminal phaseL1Input;
    [SerializeField] private Terminal phaseL1Output;

    [Header("Power Path - L2")]
    [SerializeField] private Terminal phaseL2Input;
    [SerializeField] private Terminal phaseL2Output;

    [Header("Power Path - L3")]
    [SerializeField] private Terminal phaseL3Input;
    [SerializeField] private Terminal phaseL3Output;

    [Header("Auxiliary NC Contact")]
    [SerializeField] private Terminal auxiliary95;
    [SerializeField] private Terminal auxiliary96;

    [Header("State")]
    [SerializeField] private bool isTripped;

    public bool IsTripped => isTripped;

    // Equipment's placed interaction uses IPlacedEquipmentControl on the same GameObject.
    public bool CanUse(InteractionContext context)
    {
        return isActiveAndEnabled && IsOperational;
    }

    public void Use(InteractionContext context)
    {
        if (!CanUse(context))
            return;

        if (isTripped) ResetRelay();
        else Trip();
    }

    public string GetActionText(InteractionContext context)
    {
        return isTripped
            ? "[E] Termik Röleyi Resetle"
            : "[E] Termik TEST Açması";
    }

    public override void CollectActiveLinks(List<ElectricalLink> links)
    {
        if (!IsOperational) return;

        // The thermal power tracks conduct even in the tripped state.
        // The NC 95-96 contact opens; the de-energized contactor isolates motor power.
        AddLink(links, phaseL1Input, phaseL1Output);
        AddLink(links, phaseL2Input, phaseL2Output);
        AddLink(links, phaseL3Input, phaseL3Output);

        if (!isTripped)
            AddLink(links, auxiliary95, auxiliary96);
    }

    private static void AddLink(List<ElectricalLink> links, Terminal a, Terminal b)
    {
        ElectricalLink link = new ElectricalLink(a, b);
        if (link.IsValid)
            links.Add(link);
    }

    [ContextMenu("Trip Thermal Relay")]
    public void Trip()
    {
        if (isTripped) return;
        isTripped = true;
        Debug.Log($"THERMAL RELAY | {name} | TRIPPED (TEST)", this);
        NotifyStateChanged();
    }

    [ContextMenu("Reset Thermal Relay")]
    public void ResetRelay()
    {
        if (!isTripped) return;
        isTripped = false;
        Debug.Log($"THERMAL RELAY | {name} | RESET", this);
        NotifyStateChanged();
    }
}
