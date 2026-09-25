public readonly struct CircuitValidationResult
{
    public int CorrectConnections { get; }
    public int MissingConnections { get; }
    public int IncorrectConnections { get; }
    public int RequiredConnections { get; }

    public bool IsValid =>
        RequiredConnections > 0 &&
        CorrectConnections == RequiredConnections &&
        MissingConnections == 0 &&
        IncorrectConnections == 0;

    public CircuitValidationResult(
        int correctConnections,
        int missingConnections,
        int incorrectConnections,
        int requiredConnections)
    {
        CorrectConnections =
            correctConnections;

        MissingConnections =
            missingConnections;

        IncorrectConnections =
            incorrectConnections;

        RequiredConnections =
            requiredConnections;
    }
}