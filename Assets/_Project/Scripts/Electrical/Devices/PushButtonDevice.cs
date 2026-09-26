using System.Collections.Generic;
using UnityEngine;

public sealed class PushButtonDevice :
    ElectricalDeviceBase
{
    [Header("Terminals")]
    [SerializeField]
    private Terminal terminalA;

    [SerializeField]
    private Terminal terminalB;

    [Header("Contact")]
    [SerializeField]
    private ElectricalContactType contactType =
        ElectricalContactType.NormallyOpen;

    private bool isPressed;

    public bool IsPressed =>
        isPressed;

    public bool IsContactClosed
    {
        get
        {
            if (contactType ==
                ElectricalContactType.NormallyOpen)
            {
                return isPressed;
            }

            return !isPressed;
        }
    }

    public void SetPressed(
        bool pressed)
    {
        if (isPressed == pressed)
            return;

        isPressed = pressed;

        NotifyStateChanged();
    }

    public override void CollectActiveLinks(
        List<ElectricalLink> links)
    {
        if (!IsOperational)
            return;

        if (!IsContactClosed)
            return;

        links.Add(
            new ElectricalLink(
                terminalA,
                terminalB
            )
        );
    }
}