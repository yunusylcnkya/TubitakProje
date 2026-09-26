using System.Collections.Generic;

public sealed class ElectricalSimulationSnapshot
{
    private readonly
        Dictionary<
            Terminal,
            ElectricalPotentialFlags>
        terminalStates;

    public bool HasShortCircuit { get; }

    public ElectricalSimulationSnapshot(
        Dictionary<
            Terminal,
            ElectricalPotentialFlags>
            terminalStates,
        bool hasShortCircuit)
    {
        this.terminalStates =
            new Dictionary<
                Terminal,
                ElectricalPotentialFlags>(
                    terminalStates
                );

        HasShortCircuit =
            hasShortCircuit;
    }

    public ElectricalPotentialFlags
        GetPotential(
            Terminal terminal)
    {
        if (terminal == null)
        {
            return
                ElectricalPotentialFlags.None;
        }

        if (!terminalStates.TryGetValue(
                terminal,
                out ElectricalPotentialFlags state))
        {
            return
                ElectricalPotentialFlags.None;
        }

        return state;
    }

    public bool HasPotential(
        Terminal terminal,
        ElectricalPotentialFlags potential)
    {
        ElectricalPotentialFlags state =
            GetPotential(terminal);

        return (state & potential) ==
               potential;
    }

    public bool HasCorrectDCPotential(
        Terminal positiveTerminal,
        Terminal negativeTerminal)
    {
        return
            HasPotential(
                positiveTerminal,
                ElectricalPotentialFlags.DCPositive
            )
            &&
            HasPotential(
                negativeTerminal,
                ElectricalPotentialFlags.DCNegative
            );
    }
}