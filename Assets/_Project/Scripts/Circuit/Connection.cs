public sealed class Connection
{
    public Terminal TerminalA { get; }
    public Terminal TerminalB { get; }
    public Wire Wire { get; }

    public Connection(
        Terminal terminalA,
        Terminal terminalB,
        Wire wire)
    {
        TerminalA = terminalA;
        TerminalB = terminalB;
        Wire = wire;
    }

    public bool ContainsTerminal(Terminal terminal)
    {
        return TerminalA == terminal ||
               TerminalB == terminal;
    }

    public bool Matches(
        string terminalIdA,
        string terminalIdB)
    {
        bool forward =
            TerminalA.TerminalId == terminalIdA &&
            TerminalB.TerminalId == terminalIdB;

        bool reverse =
            TerminalA.TerminalId == terminalIdB &&
            TerminalB.TerminalId == terminalIdA;

        return forward || reverse;
    }
}