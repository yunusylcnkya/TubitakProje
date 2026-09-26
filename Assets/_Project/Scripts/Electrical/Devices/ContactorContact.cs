using System;
using UnityEngine;

[Serializable]
public sealed class ContactorContact
{
    [SerializeField]
    private Terminal terminalA;

    [SerializeField]
    private Terminal terminalB;

    [SerializeField]
    private ElectricalContactType contactType =
        ElectricalContactType.NormallyOpen;

    public Terminal TerminalA =>
        terminalA;

    public Terminal TerminalB =>
        terminalB;

    public ElectricalContactType ContactType =>
        contactType;

    public bool IsClosed(
        bool coilEnergized)
    {
        if (contactType ==
            ElectricalContactType.NormallyOpen)
        {
            return coilEnergized;
        }

        return !coilEnergized;
    }
}