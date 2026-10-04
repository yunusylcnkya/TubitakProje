using UnityEngine;

public sealed class ElectricalFault
{
    public ElectricalFaultType Type { get; }

    public FaultSeverity Severity { get; }

    public string Message { get; }

    public Object Source { get; }

    public ElectricalFault(
        ElectricalFaultType type,
        FaultSeverity severity,
        string message,
        Object source = null)
    {
        Type = type;
        Severity = severity;
        Message = message;
        Source = source;
    }
}