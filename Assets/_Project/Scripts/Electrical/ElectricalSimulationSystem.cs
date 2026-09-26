using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class ElectricalSimulationSystem :
    MonoBehaviour
{
    [Header("Systems")]
    [SerializeField]
    private ConnectionManager connectionManager;

    [SerializeField]
    private PlacementManager placementManager;

    [Header("Power Sources")]
    [SerializeField]
    private DCPowerSource[] dcPowerSources;

    public event Action<
        ElectricalSimulationSnapshot>
        SimulationChanged;

    public ElectricalSimulationSnapshot
        LastSnapshot
    { get; private set; }

    private void OnEnable()
    {
        if (connectionManager != null)
        {
            connectionManager.ConnectionsChanged +=
                Recalculate;
        }

        if (placementManager != null)
        {
            placementManager.PlacementsChanged +=
                Recalculate;
        }
    }

    private void Start()
    {
        Recalculate();
    }

    private void OnDisable()
    {
        if (connectionManager != null)
        {
            connectionManager.ConnectionsChanged -=
                Recalculate;
        }

        if (placementManager != null)
        {
            placementManager.PlacementsChanged -=
                Recalculate;
        }
    }

    public void Recalculate()
    {
        Dictionary<
            Terminal,
            ElectricalPotentialFlags>
            states =
                new Dictionary<
                    Terminal,
                    ElectricalPotentialFlags>();

        if (connectionManager == null)
        {
            CreateSnapshot(
                states,
                false
            );

            return;
        }

        if (dcPowerSources != null)
        {
            foreach (
                DCPowerSource source
                in dcPowerSources)
            {
                if (source == null)
                    continue;

                if (!source.IsActive)
                    continue;

                PropagatePotential(
                    source.PositiveTerminal,
                    ElectricalPotentialFlags
                        .DCPositive,
                    states
                );

                PropagatePotential(
                    source.NegativeTerminal,
                    ElectricalPotentialFlags
                        .DCNegative,
                    states
                );
            }
        }

        bool shortCircuit =
            DetectShortCircuit(states);

        CreateSnapshot(
            states,
            shortCircuit
        );

        if (shortCircuit)
        {
            Debug.LogWarning(
                "ELECTRICAL FAULT: " +
                "DC kısa devre algılandı.",
                this
            );
        }
    }

    private void PropagatePotential(
        Terminal sourceTerminal,
        ElectricalPotentialFlags potential,
        Dictionary<
            Terminal,
            ElectricalPotentialFlags>
            states)
    {
        if (sourceTerminal == null)
            return;

        Queue<Terminal> queue =
            new Queue<Terminal>();

        HashSet<Terminal> visited =
            new HashSet<Terminal>();

        queue.Enqueue(
            sourceTerminal
        );

        visited.Add(
            sourceTerminal
        );

        while (queue.Count > 0)
        {
            Terminal current =
                queue.Dequeue();

            AddPotential(
                current,
                potential,
                states
            );

            IReadOnlyList<Connection>
                connections =
                    connectionManager
                        .Connections;

            for (
                int i = 0;
                i < connections.Count;
                i++)
            {
                Connection connection =
                    connections[i];

                if (connection == null)
                    continue;

                Terminal next = null;

                if (connection.TerminalA ==
                    current)
                {
                    next =
                        connection.TerminalB;
                }
                else if (
                    connection.TerminalB ==
                    current)
                {
                    next =
                        connection.TerminalA;
                }

                if (next == null)
                    continue;

                if (visited.Add(next))
                {
                    queue.Enqueue(next);
                }
            }
        }
    }

    private void AddPotential(
        Terminal terminal,
        ElectricalPotentialFlags potential,
        Dictionary<
            Terminal,
            ElectricalPotentialFlags>
            states)
    {
        if (terminal == null)
            return;

        if (!states.TryGetValue(
                terminal,
                out ElectricalPotentialFlags
                    currentState))
        {
            currentState =
                ElectricalPotentialFlags.None;
        }

        states[terminal] =
            currentState | potential;
    }

    private bool DetectShortCircuit(
        Dictionary<
            Terminal,
            ElectricalPotentialFlags>
            states)
    {
        foreach (
            KeyValuePair<
                Terminal,
                ElectricalPotentialFlags>
            pair
            in states)
        {
            bool hasPositive =
                (pair.Value &
                    ElectricalPotentialFlags
                        .DCPositive)
                != 0;

            bool hasNegative =
                (pair.Value &
                    ElectricalPotentialFlags
                        .DCNegative)
                != 0;

            if (hasPositive &&
                hasNegative)
            {
                return true;
            }
        }

        return false;
    }

    private void CreateSnapshot(
        Dictionary<
            Terminal,
            ElectricalPotentialFlags>
            states,
        bool shortCircuit)
    {
        LastSnapshot =
            new ElectricalSimulationSnapshot(
                states,
                shortCircuit
            );

        SimulationChanged?.Invoke(
            LastSnapshot
        );
    }

    // =========================================
    // PUBLIC QUERY
    // =========================================

    public bool HasPotential(
        Terminal terminal,
        ElectricalPotentialFlags potential)
    {
        if (LastSnapshot == null)
            return false;

        return LastSnapshot.HasPotential(
            terminal,
            potential
        );
    }
}