using System.Collections.Generic;
using UnityEngine;

public sealed class Terminal : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField]
    private string terminalId;

    [SerializeField]
    private string displayName;

    [Header("Terminal Configuration")]
    [SerializeField]
    private TerminalType terminalType;

    [Header("Electrical Configuration")]
    [SerializeField]
    private ElectricalLineType lineType =
        ElectricalLineType.Undefined;

    [Header("References")]
    [SerializeField]
    private Equipment owner;

    [Header("Connection")]
    [SerializeField]
    private Transform connectionPoint;

    [SerializeField, Min(1)]
    private int maxConnections = 1;

    private readonly List<Wire> connectedWires =
        new List<Wire>();

    public string TerminalId =>
        terminalId?.Trim();

    public string DisplayName =>
        displayName;

    public TerminalType TerminalType =>
        terminalType;

    public ElectricalLineType LineType =>
        lineType;

    public Equipment Owner =>
        owner;

    public int MaxConnections =>
        maxConnections;

    public int ConnectionCount =>
        connectedWires.Count;

    public bool IsConnected =>
        connectedWires.Count > 0;

    // Eski kodlarla uyumluluk için.
    public Wire ConnectedWire =>
        connectedWires.Count > 0
            ? connectedWires[0]
            : null;

    public IReadOnlyList<Wire> ConnectedWires =>
        connectedWires;

    public bool CanAcceptMoreConnections =>
        connectedWires.Count <
        maxConnections;

    public bool IsAvailableForWiring =>
        owner != null &&
        owner.CurrentState ==
            EquipmentState.Placed &&
        CanAcceptMoreConnections;

    public Vector3 ConnectionPosition =>
        connectionPoint != null
            ? connectionPoint.position
            : transform.position;

    private void Awake()
    {
        if (owner == null)
        {
            owner =
                GetComponentInParent<Equipment>();
        }
    }

    private void OnValidate()
    {
        if (!string.IsNullOrWhiteSpace(
                terminalId))
        {
            terminalId =
                terminalId.Trim();
        }

        maxConnections =
            Mathf.Max(1, maxConnections);
    }

    public bool TryConnect(
        Wire wire)
    {
        if (wire == null)
            return false;

        if (connectedWires.Contains(wire))
            return true;

        if (!CanAcceptMoreConnections)
            return false;

        connectedWires.Add(wire);

        return true;
    }

    public void Disconnect(
        Wire wire)
    {
        if (wire == null)
            return;

        connectedWires.Remove(wire);
    }
}