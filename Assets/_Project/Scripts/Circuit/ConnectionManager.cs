
using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class ConnectionManager : MonoBehaviour
{
    [Header("Electrical Validation")]
    [SerializeField]
    private ElectricalSimulationSystem electricalSimulation;

    [SerializeField]
    private ConnectionValidationMode validationMode =
        ConnectionValidationMode.Training;

    private readonly List<Connection> connections =
        new List<Connection>();

    public IReadOnlyList<Connection> Connections =>
        connections;

    public ConnectionValidationMode ValidationMode =>
        validationMode;

    public ElectricalSimulationSystem ElectricalSimulation =>
        electricalSimulation;

    public event Action ConnectionsChanged;

    // =========================================
    // VALIDATION
    // =========================================

    public ConnectionValidationResult ValidateConnection(
        Terminal terminalA,
        Terminal terminalB)
    {
        if (terminalA == null || terminalB == null)
        {
            return new ConnectionValidationResult(
                ConnectionIssueSeverity.Danger,
                "Bağlantı terminallerinden biri eksik.",
                false
            );
        }

        if (terminalA == terminalB)
        {
            return new ConnectionValidationResult(
                ConnectionIssueSeverity.Danger,
                "Terminal kendisine bağlanamaz.",
                false
            );
        }

        if (HasConnection(terminalA, terminalB))
        {
            return new ConnectionValidationResult(
                ConnectionIssueSeverity.Warning,
                "Bu iki terminal zaten birbirine bağlı.",
                false
            );
        }

        ElectricalSimulationSnapshot snapshot =
            electricalSimulation != null
                ? electricalSimulation.LastSnapshot
                : null;

        return ConnectionValidator.Evaluate(
            terminalA,
            terminalB,
            snapshot,
            validationMode
        );
    }

    // =========================================
    // CONNECTION REGISTRATION
    // =========================================

    public bool RegisterConnection(
        Terminal terminalA,
        Terminal terminalB,
        Wire wire)
    {
        if (wire == null)
            return false;

        ConnectionValidationResult result =
            ValidateConnection(terminalA, terminalB);

        if (!result.IsAllowed)
        {
            Debug.LogWarning(
                $"Connection reddedildi: {result.Message}",
                this
            );

            return false;
        }

        Connection connection =
            new Connection(
                terminalA,
                terminalB,
                wire
            );

        connections.Add(connection);

        Debug.Log(
            $"Connection kaydedildi: " +
            $"{terminalA.TerminalId} <-> " +
            $"{terminalB.TerminalId}",
            this
        );

        ConnectionsChanged?.Invoke();

        return true;
    }

    // =========================================
    // DUPLICATE CHECK BY TERMINAL REFERENCE
    // =========================================

    public bool HasConnection(
        Terminal terminalA,
        Terminal terminalB)
    {
        if (terminalA == null || terminalB == null)
            return false;

        foreach (Connection connection in connections)
        {
            if (connection == null)
                continue;

            bool forward =
                connection.TerminalA == terminalA &&
                connection.TerminalB == terminalB;

            bool reverse =
                connection.TerminalA == terminalB &&
                connection.TerminalB == terminalA;

            if (forward || reverse)
                return true;
        }

        return false;
    }

    // Existing API retained for compatibility.
    public bool HasConnection(
        string terminalIdA,
        string terminalIdB)
    {
        foreach (Connection connection in connections)
        {
            if (connection == null)
                continue;

            if (connection.Matches(
                terminalIdA,
                terminalIdB))
            {
                return true;
            }
        }

        return false;
    }

    // =========================================
    // LOOKUP
    // =========================================

    public Connection GetConnection(Wire wire)
    {
        foreach (Connection connection in connections)
        {
            if (connection.Wire == wire)
                return connection;
        }

        return null;
    }

    // =========================================
    // REMOVE
    // =========================================

    public void RemoveConnection(Wire wire)
    {
        Connection connection =
            GetConnection(wire);

        if (connection == null)
            return;

        connections.Remove(connection);

        ConnectionsChanged?.Invoke();
    }
}
