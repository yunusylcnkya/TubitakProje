using System;
using UnityEngine;

public sealed class CircuitValidator : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private ConnectionManager connectionManager;

    [Header("Circuit")]
    [SerializeField]
    private CircuitDefinition circuitDefinition;

    public event Action<CircuitValidationResult>
        ValidationChanged;

    private void OnEnable()
    {
        if (connectionManager != null)
        {
            connectionManager.ConnectionsChanged +=
                Validate;
        }
    }

    private void OnDisable()
    {
        if (connectionManager != null)
        {
            connectionManager.ConnectionsChanged -=
                Validate;
        }
    }

    public void Validate()
    {
        if (connectionManager == null ||
            circuitDefinition == null)
        {
            return;
        }

        RequiredConnection[] required =
            circuitDefinition.RequiredConnections;

        int correctCount = 0;

        foreach (RequiredConnection requirement in required)
        {
            bool exists =
                connectionManager.HasConnection(
                    requirement.terminalIdA,
                    requirement.terminalIdB
                );

            if (exists)
            {
                correctCount++;
            }
        }

        CircuitValidationResult result =
            new CircuitValidationResult(
                correctCount,
                required.Length
            );

        Debug.Log(
            $"Devre kontrolü: " +
            $"{result.CorrectConnections}/" +
            $"{result.RequiredConnections}"
        );

        if (result.IsValid)
        {
            Debug.Log(
                "DEVRE TAMAMLANDI ✓"
            );
        }

        ValidationChanged?.Invoke(result);
    }
}