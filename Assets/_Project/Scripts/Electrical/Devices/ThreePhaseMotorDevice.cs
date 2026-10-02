using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class ThreePhaseMotorDevice :
    ElectricalDeviceBase
{
    [Header("Motor Terminals")]
    [SerializeField]
    private Terminal terminalU1;

    [SerializeField]
    private Terminal terminalV1;

    [SerializeField]
    private Terminal terminalW1;

    [Header("Visual")]
    [SerializeField]
    private Transform rotorVisual;

    [SerializeField]
    private Vector3 rotationAxis =
        Vector3.forward;

    [SerializeField]
    private float rotationSpeed =
        360f;

    private MotorRotationDirection
        rotationDirection =
            MotorRotationDirection.Stopped;

    public MotorRotationDirection
        RotationDirection =>
            rotationDirection;

    public bool IsRunning =>
        rotationDirection ==
            MotorRotationDirection.Forward ||
        rotationDirection ==
            MotorRotationDirection.Reverse;

    public event Action<
        MotorRotationDirection>
        MotorStateChanged;

    public override void CollectActiveLinks(
        List<ElectricalLink> links)
    {
        // Motor fazları birbirine doğrudan
        // bağlanmaz.
    }

    public override bool EvaluateState(
        ElectricalSimulationSnapshot snapshot)
    {
        MotorRotationDirection newDirection =
            DetermineDirection(snapshot);

        if (newDirection ==
            rotationDirection)
        {
            return false;
        }

        rotationDirection =
            newDirection;

        Debug.Log(
            $"MOTOR | {name} | " +
            $"{rotationDirection}"
        );

        MotorStateChanged?.Invoke(
            rotationDirection
        );

        return true;
    }

    private MotorRotationDirection
        DetermineDirection(
            ElectricalSimulationSnapshot snapshot)
    {
        if (!IsOperational ||
            snapshot == null)
        {
            return
                MotorRotationDirection.Stopped;
        }

        if (snapshot.HasShortCircuit)
        {
            return
                MotorRotationDirection.Invalid;
        }

        int u =
            GetPhase(
                snapshot,
                terminalU1
            );

        int v =
            GetPhase(
                snapshot,
                terminalV1
            );

        int w =
            GetPhase(
                snapshot,
                terminalW1
            );

        if (u == 0 ||
            v == 0 ||
            w == 0)
        {
            return
                MotorRotationDirection.Stopped;
        }

        if (u == v ||
            v == w ||
            u == w)
        {
            return
                MotorRotationDirection.Invalid;
        }

        bool forward =
            (u == 1 && v == 2 && w == 3) ||
            (u == 2 && v == 3 && w == 1) ||
            (u == 3 && v == 1 && w == 2);

        return forward
            ? MotorRotationDirection.Forward
            : MotorRotationDirection.Reverse;
    }

    private int GetPhase(
        ElectricalSimulationSnapshot snapshot,
        Terminal terminal)
    {
        ElectricalPotentialFlags state =
            snapshot.GetPotential(
                terminal
            );

        bool l1 =
            (state &
             ElectricalPotentialFlags.PhaseL1) != 0;

        bool l2 =
            (state &
             ElectricalPotentialFlags.PhaseL2) != 0;

        bool l3 =
            (state &
             ElectricalPotentialFlags.PhaseL3) != 0;

        int count = 0;

        if (l1) count++;
        if (l2) count++;
        if (l3) count++;

        if (count != 1)
            return 0;

        if (l1) return 1;
        if (l2) return 2;

        return 3;
    }

    private void Update()
    {
        if (!IsRunning ||
            rotorVisual == null)
        {
            return;
        }

        float direction =
            rotationDirection ==
                MotorRotationDirection.Forward
                ? 1f
                : -1f;

        rotorVisual.Rotate(
            rotationAxis,
            rotationSpeed *
            direction *
            Time.deltaTime,
            Space.Self
        );
    }
}