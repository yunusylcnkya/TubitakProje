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
}