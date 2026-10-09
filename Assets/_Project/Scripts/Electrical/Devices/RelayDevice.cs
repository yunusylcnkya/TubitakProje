using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class RelayDevice :
    ElectricalDeviceBase
{
    [Header("Coil")]
    [SerializeField]
    private Terminal coilA1;

    [SerializeField]
    private Terminal coilA2;

    [SerializeField]
    private bool polaritySensitive = true;

    [Header("Contact 1 - 11 / 12 / 14")]
    [SerializeField]
    private Terminal common11;

    [SerializeField]
    private Terminal nc12;

    [SerializeField]
    private Terminal no14;

    [Header("Contact 2 - 21 / 22 / 24")]
    [SerializeField]
    private Terminal common21;

    [SerializeField]
    private Terminal nc22;

    [SerializeField]
    private Terminal no24;

    private bool isEnergized;

    public bool IsEnergized =>
        isEnergized;

    public Terminal CoilA1 =>
        coilA1;

    public Terminal CoilA2 =>
        coilA2;

    public event Action<bool>
        EnergizedChanged;

    public override void CollectActiveLinks(
        List<ElectricalLink> links)
    {
        if (!IsOperational)
            return;

        if (links == null)
            return;

        if (isEnergized)
        {
            // Röle çekili:
            // 11-14 kapalı
            // 21-24 kapalı
            AddLink(
                links,
                common11,
                no14
            );

            AddLink(
                links,
                common21,
                no24
            );
        }
        else
        {
            // Röle bırakılmış:
            // 11-12 kapalı
            // 21-22 kapalı
            AddLink(
                links,
                common11,
                nc12
            );

            AddLink(
                links,
                common21,
                nc22
            );
        }
    }

    public override bool EvaluateState(
        ElectricalSimulationSnapshot snapshot)
    {
        bool shouldBeEnergized =
            false;

        if (IsOperational &&
            snapshot != null &&
            !snapshot.HasShortCircuit &&
            coilA1 != null &&
            coilA2 != null)
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
            $"RELAY | {name} | " + $"{(isEnergized ? "ENERGIZED" : "RELEASED")}"
        );

        EnergizedChanged?.Invoke(
            isEnergized
        );

        return true;
    }

    private static void AddLink(
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
}