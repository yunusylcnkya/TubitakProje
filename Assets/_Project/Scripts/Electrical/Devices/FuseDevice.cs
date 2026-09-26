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

    [Header("State")]
    [SerializeField]
    private bool isBlown;

    public bool IsBlown =>
        isBlown;

    public override void CollectActiveLinks(
        List<ElectricalLink> links)
    {
        if (!IsOperational)
            return;

        if (isBlown)
            return;

        links.Add(
            new ElectricalLink(
                inputTerminal,
                outputTerminal
            )
        );
    }

    public void Blow()
    {
        if (isBlown)
            return;

        isBlown = true;

        NotifyStateChanged();
    }

    public void ResetFuse()
    {
        if (!isBlown)
            return;

        isBlown = false;

        NotifyStateChanged();
    }
}