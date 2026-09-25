public readonly struct CircuitValidationResult
{
    public int CorrectConnections { get; }
    public int RequiredConnections { get; }

    public bool IsValid =>
        RequiredConnections > 0 &&
        CorrectConnections == RequiredConnections;

    public CircuitValidationResult(
        int correctConnections,
        int requiredConnections)
    {
        CorrectConnections =
            correctConnections;

        RequiredConnections =
            requiredConnections;
    }
}