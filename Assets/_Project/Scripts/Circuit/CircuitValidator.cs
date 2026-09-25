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

    public CircuitValidationResult LastResult { get; private set; }

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

        int correctCount = 0;
        int incorrectCount = 0;

        // Gerekli bağlantıların kaç tanesi var?
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

        // Oyuncunun yaptığı bağlantılardan
        // kaç tanesi bu devrede istenmiyor?
        foreach (Connection connection
                 in connectionManager.Connections)
        {
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