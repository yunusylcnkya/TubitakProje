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

    public CircuitValidationResult LastResult
    {
        get;
        private set;
    }

    // Stage 16 FaultSystem buradan
    // aktif CircuitDefinition'a ulaşacak.
    public CircuitDefinition Definition =>
        circuitDefinition;

    private void OnEnable()
    {
        if (connectionManager != null)
        {
            connectionManager.ConnectionsChanged +=
                Validate;
        }
    }

    private void Start()
    {
        Validate();
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

        if (required == null)
        {
            Debug.LogWarning(
                "CircuitDefinition içerisinde " +
                "Required Connections tanımlanmamış.",
                circuitDefinition
            );

            return;
        }

        int correctCount = 0;
        int incorrectCount = 0;

        // =====================================
        // GEREKLİ BAĞLANTILAR
        // =====================================

        foreach (
            RequiredConnection requirement
            in required)
        {
            if (requirement == null)
                continue;

            bool exists =
                connectionManager.HasConnection(
                    requirement.TerminalIdA,
                    requirement.TerminalIdB
                );

            if (exists)
            {
                correctCount++;
            }
        }

        // =====================================
        // OYUNCUNUN YAPTIĞI YANLIŞ BAĞLANTILAR
        // =====================================

        foreach (
            Connection connection
            in connectionManager.Connections)
        {
            if (connection == null)
                continue;

            if (connection.TerminalA == null ||
                connection.TerminalB == null)
            {
                continue;
            }

            bool requiredConnection =
                circuitDefinition.IsRequiredConnection(
                    connection.TerminalA.TerminalId,
                    connection.TerminalB.TerminalId
                );

            if (!requiredConnection)
            {
                incorrectCount++;
            }
        }

        int missingCount =
            required.Length - correctCount;

        LastResult =
            new CircuitValidationResult(
                correctCount,
                missingCount,
                incorrectCount,
                required.Length
            );

        Debug.Log(
            $"DEVRE KONTROLÜ | " +
            $"Doğru: {LastResult.CorrectConnections} | " +
            $"Eksik: {LastResult.MissingConnections} | " +
            $"Yanlış: {LastResult.IncorrectConnections}"
        );

        if (LastResult.IsValid)
        {
            Debug.Log(
                "DEVRE DOĞRU VE TAMAMLANDI ✓"
            );
        }
        else
        {
            Debug.Log(
                "DEVRE HENÜZ GEÇERLİ DEĞİL."
            );
        }

        ValidationChanged?.Invoke(
            LastResult
        );
    }
}