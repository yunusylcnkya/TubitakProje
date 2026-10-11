using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class TimeRelayDevice :
    ElectricalDeviceBase
{
    [Header("Coil")]
    [SerializeField]
    private Terminal coilA1;

    [SerializeField]
    private Terminal coilA2;

    [SerializeField]
    private bool polaritySensitive = true;

    [Header("Timed Changeover Contact")]
    [SerializeField]
    private Terminal common15;

    [SerializeField]
    private Terminal nc16;

    [SerializeField]
    private Terminal no18;

    [Header("TON Settings")]
    [SerializeField, Min(0.1f)]
    private float delaySeconds = 3f;

    private TimeRelayState state =
        TimeRelayState.Idle;

    private float elapsedTime;
    private bool coilPowered;

    public TimeRelayState State =>
        state;

    public bool CoilPowered =>
        coilPowered;

    public bool IsTiming =>
        state == TimeRelayState.Timing;

    public bool IsElapsed =>
        state == TimeRelayState.Elapsed;

    public float DelaySeconds =>
        delaySeconds;

    public float ElapsedTime =>
        elapsedTime;

    public float NormalizedProgress =>
        delaySeconds > 0f
            ? Mathf.Clamp01(
                elapsedTime / delaySeconds
            )
            : 0f;

    public event Action<TimeRelayState>
        TimeRelayStateChanged;

    public override void CollectActiveLinks(
        List<ElectricalLink> links)
    {
        if (!IsOperational)
            return;

        if (links == null)
            return;

        if (state == TimeRelayState.Elapsed)
        {
            AddLink(
                links,
                common15,
                no18
            );
        }
        else
        {
            AddLink(
                links,
                common15,
                nc16
            );
        }
    }

    public override bool EvaluateState(
        ElectricalSimulationSnapshot snapshot)
    {
        bool newCoilPowered =
            DetermineCoilPower(
                snapshot
            );

        coilPowered =
            newCoilPowered;

        // Bobin enerjisi yoksa zaman rölesi
        // normal durumuna anında döner.
        if (!coilPowered)
        {
            if (state ==
                    TimeRelayState.Idle &&
                Mathf.Approximately(
                    elapsedTime,
                    0f))
            {
                return false;
            }

            elapsedTime = 0f;

            ChangeState(
                TimeRelayState.Idle
            );

            return true;
        }

        // Bobin yeni enerji aldı.
        if (state ==
            TimeRelayState.Idle)
        {
            elapsedTime = 0f;

            ChangeState(
                TimeRelayState.Timing
            );

            return true;
        }

        return false;
    }

    private void Update()
    {
        if (!IsOperational)
        {
            return;
        }

        if (!coilPowered)
        {
            return;
        }

        if (state !=
            TimeRelayState.Timing)
        {
            return;
        }

        elapsedTime +=
            Time.deltaTime;

        if (elapsedTime <
            delaySeconds)
        {
            return;
        }

        elapsedTime =
            delaySeconds;

        ChangeState(
            TimeRelayState.Elapsed
        );

        // Süre Update içinde dolduğu için
        // elektrik solver'ına yeniden hesaplama
        // gerektiğini bildiriyoruz.
        NotifyStateChanged();
    }

    private bool DetermineCoilPower(
        ElectricalSimulationSnapshot snapshot)
    {
        if (!IsOperational ||
            snapshot == null ||
            snapshot.HasShortCircuit ||
            coilA1 == null ||
            coilA2 == null)
        {
            return false;
        }

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

        if (polaritySensitive)
        {
            return normal;
        }

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

        return normal ||
               reversed;
    }

    private void ChangeState(
        TimeRelayState newState)
    {
        if (state ==
            newState)
        {
            return;
        }

        state =
            newState;

        Debug.Log(
            $"TIME RELAY | {name} | " +
            $"{state} | " +
            $"{elapsedTime:0.00}/{delaySeconds:0.00}s"
        );

        TimeRelayStateChanged?.Invoke(
            state
        );
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