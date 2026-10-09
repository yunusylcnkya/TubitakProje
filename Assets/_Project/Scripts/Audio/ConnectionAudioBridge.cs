using UnityEngine;

public sealed class ConnectionAudioBridge :
    MonoBehaviour
{
    [SerializeField]
    private ConnectionManager connectionManager;

    private int previousConnectionCount;

    private void OnEnable()
    {
        if (connectionManager == null)
            return;

        previousConnectionCount =
            connectionManager.Connections.Count;

        connectionManager.ConnectionsChanged +=
            HandleConnectionsChanged;
    }

    private void OnDisable()
    {
        if (connectionManager != null)
        {
            connectionManager.ConnectionsChanged -=
                HandleConnectionsChanged;
        }
    }

    private void HandleConnectionsChanged()
    {
        if (connectionManager == null)
            return;

        int currentCount =
            connectionManager.Connections.Count;

        if (currentCount >
            previousConnectionCount)
        {
            AudioManager.Instance?.Play(
                GameAudioCue.WireConnected
            );
        }
        else if (currentCount <
                 previousConnectionCount)
        {
            AudioManager.Instance?.Play(
                GameAudioCue.WireRemoved
            );
        }

        previousConnectionCount =
            currentCount;
    }
}