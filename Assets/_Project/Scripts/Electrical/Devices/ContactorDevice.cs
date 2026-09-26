using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class ContactorDevice :
    ElectricalDeviceBase
{
    [Header("Coil")]
    [SerializeField]
    private Terminal coilA1;

    [SerializeField]
    private Terminal coilA2;

    [SerializeField]
    private bool polaritySensitive = true;

    [Header("Contacts")]
    [SerializeField]
    private ContactorContact[] contacts;

    private bool isEnergized;

    public bool IsEnergized =>
        isEnergized;

    public event Action<bool>
        EnergizedChanged;

    public override void CollectActiveLinks(
        List<ElectricalLink> links)
    {
        if (!IsOperational)
            return;

        if (contacts == null)
            return;

        foreach (
            ContactorContact contact
            in contacts)
        {
            if (contact == null)
                continue;

            if (!contact.IsClosed(
                    isEnergized))
            {
                continue;
            }

            ElectricalLink link =
                new ElectricalLink(
                    contact.TerminalA,
                    contact.TerminalB
                );

            if (link.IsValid)
            {
                links.Add(link);
            }
        }
    }

    public override bool EvaluateState(
        ElectricalSimulationSnapshot snapshot)
    {
        bool shouldBeEnergized =
            false;

        if (IsOperational &&
            snapshot != null &&
            !snapshot.HasShortCircuit)
        {
            if (polaritySensitive)
            {
                shouldBeEnergized =
                    snapshot.HasPotential(
                        coilA1,
                        ElectricalPotentialFlags
                            .DCPositive
                    )
                    &&
                    snapshot.HasPotential(
                        coilA2,
                        ElectricalPotentialFlags
                            .DCNegative
                    );
            }
            else
            {
                bool normal =
                    snapshot.HasPotential(
                        coilA1,
                        ElectricalPotentialFlags
                            .DCPositive
                    )
                    &&
                    snapshot.HasPotential(
                        coilA2,
                        ElectricalPotentialFlags
                            .DCNegative
                    );

                bool reversed =
                    snapshot.HasPotential(
                        coilA1,
                        ElectricalPotentialFlags
                            .DCNegative
                    )
                    &&
                    snapshot.HasPotential(
                        coilA2,
                        ElectricalPotentialFlags
                            .DCPositive
                    );

                shouldBeEnergized =
                    normal || reversed;
            }
        }

        if (shouldBeEnergized ==
            isEnergized)
        {
            return false;
        }

        isEnergized =
            shouldBeEnergized;

        Debug.Log(
            $"CONTACTOR | {name} | " +
            $"{(isEnergized ? "ENERGIZED" : "RELEASED")}");

        EnergizedChanged?.Invoke(
            isEnergized
        );

        return true;
    }
}