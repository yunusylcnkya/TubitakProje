using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class ConnectionManager : MonoBehaviour
{
    private readonly List<Connection> connections =
        new List<Connection>();

    public IReadOnlyList<Connection> Connections =>
        connections;

    public event Action ConnectionsChanged;

    public bool RegisterConnection(
        Terminal terminalA,
        Terminal terminalB,
        Wire wire)
    {
        if (terminalA == null ||
            terminalB == null ||
            wire == null)
        {
            return false;
        }

        if (HasConnection(
                terminalA.TerminalId,
                terminalB.TerminalId))
        {
            Debug.LogWarning(
                "Bu bağlantı zaten kayıtlı."
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
            $"Connection kayıt edildi: " +
            $"{terminalA.TerminalId} <-> " +
            $"{terminalB.TerminalId}"
        );

        ConnectionsChanged?.Invoke();

        return true;
    }

    public bool HasConnection(
        string terminalIdA,
        string terminalIdB)
    {
        foreach (Connection connection in connections)
        {
            if (connection.Matches(
                    terminalIdA,
                    terminalIdB))
            {
                return true;
            }
        }

        return false;
    }

    public Connection GetConnection(Wire wire)
    {
        foreach (Connection connection in connections)
        {
            if (connection.Wire == wire)
                return connection;
        }

        return null;
    }

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