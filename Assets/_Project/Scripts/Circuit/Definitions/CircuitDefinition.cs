using UnityEngine;

[CreateAssetMenu(
    fileName = "CD_Circuit",
    menuName = "Industrial Automation/Circuit/Circuit Definition"
)]
public sealed class CircuitDefinition :
    ScriptableObject
{
    [Header("Identity")]
    [SerializeField]
    private string circuitId;

    [SerializeField]
    private string displayName;

    [Header("Required Connections")]
    [SerializeField]
    private RequiredConnection[]
        requiredConnections;

    public string CircuitId =>
        circuitId?.Trim();

    public string DisplayName =>
        displayName;

    public RequiredConnection[]
        RequiredConnections =>
            requiredConnections;

    public int RequiredConnectionCount =>
        requiredConnections != null
            ? requiredConnections.Length
            : 0;

    public bool IsRequiredConnection(
        string terminalIdA,
        string terminalIdB)
    {
        if (requiredConnections == null)
            return false;

        foreach (
            RequiredConnection required
            in requiredConnections)
        {
            if (required == null)
                continue;

            if (required.Matches(
                    terminalIdA,
                    terminalIdB))
            {
                return true;
            }
        }

        return false;
    }

    private void OnValidate()
    {
        if (!string.IsNullOrWhiteSpace(
                circuitId))
        {
            circuitId =
                circuitId.Trim();
        }
    }
}