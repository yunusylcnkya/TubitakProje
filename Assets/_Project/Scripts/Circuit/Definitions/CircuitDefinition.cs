using UnityEngine;

[CreateAssetMenu(
    fileName = "CircuitDefinition",
    menuName = "Industrial Automation/Circuit/Circuit Definition"
)]
public sealed class CircuitDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField]
    private string circuitId;

    [SerializeField]
    private string displayName;

    [Header("Required Connections")]
    [SerializeField]
    private RequiredConnection[] requiredConnections;

    public string CircuitId => circuitId;
    public string DisplayName => displayName;

    public RequiredConnection[] RequiredConnections =>
        requiredConnections;

    public bool IsRequiredConnection(
string terminalIdA,
string terminalIdB)
    {
        foreach (RequiredConnection requirement
                 in requiredConnections)
        {
            bool forward =
                requirement.terminalIdA == terminalIdA &&
                requirement.terminalIdB == terminalIdB;

            bool reverse =
                requirement.terminalIdA == terminalIdB &&
                requirement.terminalIdB == terminalIdA;

            if (forward || reverse)
                return true;
        }

        return false;
    }
}