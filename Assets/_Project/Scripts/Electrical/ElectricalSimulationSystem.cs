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

    [Header("DC Power Sources")]
    [SerializeField]
    private DCPowerSource[] dcPowerSources;

    [Header("Three Phase Power Sources")]
    [SerializeField]
    private ThreePhasePowerSource[]
        threePhasePowerSources;

    [Header("Electrical Devices")]
    [SerializeField]
    private ElectricalDeviceBase[]
        electricalDevices;

    [Header("Solver")]
    [SerializeField, Min(1)]
    private int maxSolverIterations = 12;

    public event Action<
        ElectricalSimulationSnapshot>
        SimulationChanged;

    public ElectricalSimulationSnapshot
        LastSnapshot
    { get; private set; }

    private bool isRecalculating;
    private bool recalculateRequested;

    private void OnEnable()
    {
        if (connectionManager != null)
        {
            connectionManager
                .ConnectionsChanged +=
                Recalculate;
        }

        if (placementManager != null)
        {
            placementManager
                .PlacementsChanged +=
                Recalculate;
        }

        SubscribeDevices();
    }

    private void Start()
    {
        Recalculate();
    }

    private void OnDisable()
    {
        if (connectionManager != null)
        {
            connectionManager
                .ConnectionsChanged -=
                Recalculate;
        }

        if (placementManager != null)
        {
            placementManager
                .PlacementsChanged -=
                Recalculate;
        }

        UnsubscribeDevices();
    }

    private void SubscribeDevices()
    {
        if (electricalDevices == null)
            return;

        foreach (
            ElectricalDeviceBase device
            in electricalDevices)
        {
            if (device == null)
                continue;

            device.StateChanged +=
                HandleDeviceStateChanged;
        }
    }

    private void UnsubscribeDevices()
    {
        if (electricalDevices == null)
            return;

        foreach (
            ElectricalDeviceBase device
            in electricalDevices)
        {
            if (device == null)
                continue;

            device.StateChanged -=
                HandleDeviceStateChanged;
        }
    }

    private void HandleDeviceStateChanged()
    {
        Recalculate();
    }

    public void Recalculate()
    {
        if (isRecalculating)
        {
            recalculateRequested = true;
            return;
        }

        isRecalculating = true;

        do
        {
            recalculateRequested = false;

            SolveUntilStable();
        }
        while (recalculateRequested);

        isRecalculating = false;
    }

    private void SolveUntilStable()
    {
        ElectricalSimulationSnapshot snapshot =
            null;

        bool stable = false;

        for (
            int iteration = 0;
            iteration < maxSolverIterations;
            iteration++)
        {
            snapshot =
                BuildSnapshot();

            bool deviceChanged =
                EvaluateDevices(snapshot);

            if (!deviceChanged)
            {
                stable = true;
                break;
            }
        }

        if (!stable)
        {
            Debug.LogWarning(
                "Electrical solver maksimum " +
                "iterasyon sayısına ulaştı.",
                this
            );

            snapshot =
                BuildSnapshot();
        }

        LastSnapshot = snapshot;

        SimulationChanged?.Invoke(
            LastSnapshot
        );

        if (LastSnapshot != null &&
            LastSnapshot.HasShortCircuit)
        {
            Debug.LogWarning(
                "ELECTRICAL FAULT: " +
                "Kısa devre algılandı.",
                this
            );
        }
    }

    private ElectricalSimulationSnapshot
        BuildSnapshot()
    {
        Dictionary<
            Terminal,
            List<Terminal>>
            adjacency =
                BuildAdjacency();

        Dictionary<
            Terminal,
            ElectricalPotentialFlags>
            states =
                new Dictionary<
                    Terminal,
                    ElectricalPotentialFlags>();

        // =============================
        // DC SOURCES
        // =============================

        if (dcPowerSources != null)
        {
            foreach (
                DCPowerSource source
                in dcPowerSources)
            {
                if (source == null ||
                    !source.IsActive)
                {
                    continue;
                }

                PropagatePotential(
                    source.PositiveTerminal,
                    ElectricalPotentialFlags
                        .DCPositive,
                    adjacency,
                    states
                );

                PropagatePotential(
                    source.NegativeTerminal,
                    ElectricalPotentialFlags
                        .DCNegative,
                    adjacency,
                    states
                );
            }
        }

        // =============================
        // THREE PHASE SOURCES
        // =============================

        if (threePhasePowerSources != null)
        {
            foreach (
                ThreePhasePowerSource source
                in threePhasePowerSources)
            {
                if (source == null ||
                    !source.IsActive)
                {
                    continue;
                }

                PropagatePotential(
                    source.PhaseL1Terminal,
                    ElectricalPotentialFlags.PhaseL1,
                    adjacency,
                    states
                );

                PropagatePotential(
                    source.PhaseL2Terminal,
                    ElectricalPotentialFlags.PhaseL2,
                    adjacency,
                    states
                );

                PropagatePotential(
                    source.PhaseL3Terminal,
                    ElectricalPotentialFlags.PhaseL3,
                    adjacency,
                    states
                );
            }
        }

        bool hasShortCircuit =
            DetectShortCircuit(states);

        return
            new ElectricalSimulationSnapshot(
                states,
                hasShortCircuit
            );
    }

    private Dictionary<
        Terminal,
        List<Terminal>>
        BuildAdjacency()
    {
        Dictionary<
            Terminal,
            List<Terminal>>
            adjacency =
                new Dictionary<
                    Terminal,
                    List<Terminal>>();

        // Fiziksel kablolar
        if (connectionManager != null)
        {
            IReadOnlyList<Connection>
                connections =
                    connectionManager.Connections;

            for (
                int i = 0;
                i < connections.Count;
                i++)
            {
                Connection connection =
                    connections[i];

                if (connection == null)
                    continue;

                AddBidirectionalLink(
                    adjacency,
                    connection.TerminalA,
                    connection.TerminalB
                );
            }
        }

        // Cihaz iç bağlantıları
        if (electricalDevices != null)
        {
            List<ElectricalLink> links =
                new List<ElectricalLink>();

            foreach (
                ElectricalDeviceBase device
                in electricalDevices)
            {
                if (device == null ||
                    !device.isActiveAndEnabled)
                {
                    continue;
                }

                device.CollectActiveLinks(
                    links
                );
            }

            foreach (
                ElectricalLink link
                in links)
            {
                if (!link.IsValid)
                    continue;

                AddBidirectionalLink(
                    adjacency,
                    link.TerminalA,
                    link.TerminalB
                );
            }
        }

        return adjacency;
    }

    private void AddBidirectionalLink(
        Dictionary<
            Terminal,
            List<Terminal>>
            adjacency,
        Terminal terminalA,
        Terminal terminalB)
    {
        if (terminalA == null ||
            terminalB == null)
        {
            return;
        }

        AddNeighbour(
            adjacency,
            terminalA,
            terminalB
        );

        AddNeighbour(
            adjacency,
            terminalB,
            terminalA
        );
    }

    private void AddNeighbour(
        Dictionary<
            Terminal,
            List<Terminal>>
            adjacency,
        Terminal source,
        Terminal target)
    {
        if (!adjacency.TryGetValue(
                source,
                out List<Terminal> neighbours))
        {
            neighbours =
                new List<Terminal>();

            adjacency[source] =
                neighbours;
        }

        if (!neighbours.Contains(target))
        {
            neighbours.Add(target);
        }
    }

    private void PropagatePotential(
        Terminal sourceTerminal,
        ElectricalPotentialFlags potential,
        Dictionary<
            Terminal,
            List<Terminal>>
            adjacency,
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

        queue.Enqueue(sourceTerminal);
        visited.Add(sourceTerminal);

        while (queue.Count > 0)
        {
            Terminal current =
                queue.Dequeue();

            AddPotential(
                current,
                potential,
                states
            );

            if (!adjacency.TryGetValue(
                    current,
                    out List<Terminal>
                        neighbours))
            {
                continue;
            }

            foreach (
                Terminal neighbour
                in neighbours)
            {
                if (neighbour == null)
                    continue;

                if (visited.Add(neighbour))
                {
                    queue.Enqueue(
                        neighbour
                    );
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
                    current))
        {
            current =
                ElectricalPotentialFlags.None;
        }

        states[terminal] =
            current | potential;
    }

    private bool EvaluateDevices(
        ElectricalSimulationSnapshot snapshot)
    {
        if (electricalDevices == null)
            return false;

        bool changed = false;

        foreach (
            ElectricalDeviceBase device
            in electricalDevices)
        {
            if (device == null ||
                !device.isActiveAndEnabled)
            {
                continue;
            }

            if (device.EvaluateState(snapshot))
            {
                changed = true;
            }
        }

        return changed;
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
            int sourcePotentialCount = 0;

            if ((pair.Value &
                 ElectricalPotentialFlags.DCPositive) != 0)
            {
                sourcePotentialCount++;
            }

            if ((pair.Value &
                 ElectricalPotentialFlags.DCNegative) != 0)
            {
                sourcePotentialCount++;
            }

            if ((pair.Value &
                 ElectricalPotentialFlags.PhaseL1) != 0)
            {
                sourcePotentialCount++;
            }

            if ((pair.Value &
                 ElectricalPotentialFlags.PhaseL2) != 0)
            {
                sourcePotentialCount++;
            }

            if ((pair.Value &
                 ElectricalPotentialFlags.PhaseL3) != 0)
            {
                sourcePotentialCount++;
            }

            if (sourcePotentialCount > 1)
            {
                return true;
            }
        }

        return false;
    }

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