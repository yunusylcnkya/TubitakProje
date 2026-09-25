using UnityEngine;

public sealed class Terminal : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private string terminalId;
    [SerializeField] private string displayName;

    [Header("Terminal Configuration")]
    [SerializeField] private TerminalType terminalType;

    [Header("Electrical Configuration")]
    [SerializeField]
    private ElectricalLineType lineType =
        ElectricalLineType.Undefined;

    [Header("References")]
    [SerializeField] private Equipment owner;

    [Header("Connection")]
    [SerializeField] private Transform connectionPoint;

    private Wire connectedWire;

    public string TerminalId => terminalId;
    public string DisplayName => displayName;

    public TerminalType TerminalType => terminalType;
    public ElectricalLineType LineType => lineType;

    public Equipment Owner => owner;

    public Wire ConnectedWire => connectedWire;

    public bool IsConnected =>
        connectedWire != null;

    public bool IsAvailableForWiring =>
        owner != null &&
        owner.CurrentState == EquipmentState.Placed &&
        !IsConnected;

    public Vector3 ConnectionPosition =>
        connectionPoint != null
            ? connectionPoint.position
            : transform.position;

    private void Awake()
    {
        if (owner == null)
        {
            owner = GetComponentInParent<Equipment>();
        }
    }

    public bool TryConnect(Wire wire)
    {
        if (wire == null)
            return false;

        if (IsConnected)
            return false;

        connectedWire = wire;

        return true;
    }

    public void Disconnect(Wire wire)
    {
        if (connectedWire != wire)
            return;

        connectedWire = null;
    }
}