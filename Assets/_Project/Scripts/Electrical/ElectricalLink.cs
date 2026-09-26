public readonly struct ElectricalLink
{
    public Terminal TerminalA { get; }
    public Terminal TerminalB { get; }

    public bool IsValid =>
        TerminalA != null &&
        TerminalB != null &&
        TerminalA != TerminalB;

    public ElectricalLink(
        Terminal terminalA,
        Terminal terminalB)
    {
        TerminalA = terminalA;
        TerminalB = terminalB;
    }
}