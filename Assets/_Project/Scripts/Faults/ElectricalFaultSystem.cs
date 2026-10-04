using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class ElectricalFaultSystem :
    MonoBehaviour
{
    [Header("Core")]
    [SerializeField]
    private ElectricalSimulationSystem
        electricalSimulation;

    [SerializeField]
    private ConnectionManager connectionManager;

    [SerializeField]
    private CircuitDefinition circuitDefinition;

    [Header("Monitored Devices")]
    [SerializeField]
    private ContactorDevice monitoredContactor;

    [SerializeField]
    private ThreePhaseMotorDevice monitoredMotor;

    [SerializeField]
    private ThermalRelayDevice monitoredThermalRelay;

    [SerializeField]
    private FuseDevice controlFuse;

    private readonly List<ElectricalFault>
        activeFaults =
            new List<ElectricalFault>();

    public IReadOnlyList<ElectricalFault>
        ActiveFaults =>
            activeFaults;

    public event Action FaultsChanged;

    private void OnEnable()
    {
        if (electricalSimulation != null)
        {
            electricalSimulation
                .SimulationChanged +=
                HandleSimulationChanged;
        }
    }

    private void Start()
    {
        EvaluateFaults(
            electricalSimulation != null
                ? electricalSimulation.LastSnapshot
                : null
        );
    }

    private void OnDisable()
    {
        if (electricalSimulation != null)
        {
            electricalSimulation
                .SimulationChanged -=
                HandleSimulationChanged;
        }
    }

    private void HandleSimulationChanged(
        ElectricalSimulationSnapshot snapshot)
    {
        EvaluateFaults(snapshot);
    }

    private void EvaluateFaults(
        ElectricalSimulationSnapshot snapshot)
    {
        List<ElectricalFault> faults =
            new List<ElectricalFault>();

        if (snapshot == null)
        {
            ReplaceFaults(faults);
            return;
        }

        DetectShortCircuit(
            snapshot,
            faults
        );

        DetectProtectionFaults(
            faults
        );

        DetectMotorFaults(
            snapshot,
            faults
        );

        DetectConnectionFaults(
            faults
        );

        ReplaceFaults(faults);
    }

    // =========================================
    // SHORT CIRCUIT
    // =========================================

    private void DetectShortCircuit(
        ElectricalSimulationSnapshot snapshot,
        List<ElectricalFault> faults)
    {
        if (!snapshot.HasShortCircuit)
            return;

        faults.Add(
            new ElectricalFault(
                ElectricalFaultType.ShortCircuit,
                FaultSeverity.Danger,
                "Elektrik devresinde kısa devre algılandı.",
                electricalSimulation
            )
        );
    }

    // =========================================
    // PROTECTION
    // =========================================

    private void DetectProtectionFaults(
        List<ElectricalFault> faults)
    {
        if (monitoredThermalRelay != null &&
            monitoredThermalRelay.IsTripped)
        {
            faults.Add(
                new ElectricalFault(
                    ElectricalFaultType.ThermalTrip,
                    FaultSeverity.Warning,
                    "Termik röle açtı. Motor koruma devresi aktif.",
                    monitoredThermalRelay
                )
            );
        }

        if (controlFuse != null &&
            controlFuse.IsBlown)
        {
            string reason =
                string.IsNullOrWhiteSpace(
                    controlFuse.LastTripReason)
                    ? "Bilinmeyen neden"
                    : controlFuse.LastTripReason;

            faults.Add(
                new ElectricalFault(
                    ElectricalFaultType.FuseBlown,
                    FaultSeverity.Danger,
                    $"Kontrol devresi sigortası attı. Sebep: {reason}",
                    controlFuse
                )
            );
        }
    }

    // =========================================
    // MOTOR
    // =========================================

    private void DetectMotorFaults(
        ElectricalSimulationSnapshot snapshot,
        List<ElectricalFault> faults)
    {
        if (monitoredMotor == null)
            return;

        if (monitoredMotor.RotationDirection ==
            MotorRotationDirection.Reverse)
        {
            faults.Add(
                new ElectricalFault(
                    ElectricalFaultType
                        .WrongPhaseSequence,
                    FaultSeverity.Warning,
                    "Motor ters yönde dönüyor. Faz sırasını kontrol edin.",
                    monitoredMotor
                )
            );
        }

        if (monitoredMotor.RotationDirection ==
            MotorRotationDirection.Invalid)
        {
            faults.Add(
                new ElectricalFault(
                    ElectricalFaultType
                        .InvalidPhaseState,
                    FaultSeverity.Danger,
                    "Motor terminallerinde geçersiz faz durumu algılandı.",
                    monitoredMotor
                )
            );
        }

        if (monitoredContactor == null ||
            !monitoredContactor.IsEnergized)
        {
            return;
        }

        if (snapshot.HasShortCircuit)
            return;

        bool uPowered =
            HasAnyPhase(
                snapshot,
                monitoredMotor.TerminalU1
            );

        bool vPowered =
            HasAnyPhase(
                snapshot,
                monitoredMotor.TerminalV1
            );

        bool wPowered =
            HasAnyPhase(
                snapshot,
                monitoredMotor.TerminalW1
            );

        int poweredPhaseCount = 0;

        if (uPowered) poweredPhaseCount++;
        if (vPowered) poweredPhaseCount++;
        if (wPowered) poweredPhaseCount++;

        if (poweredPhaseCount == 0)
        {
            faults.Add(
                new ElectricalFault(
                    ElectricalFaultType.OpenCircuit,
                    FaultSeverity.Danger,
                    "Kontaktör çekili olmasına rağmen motora güç ulaşmıyor. Açık devre kontrolü yapın.",
                    monitoredMotor
                )
            );

            return;
        }

        if (poweredPhaseCount < 3)
        {
            faults.Add(
                new ElectricalFault(
                    ElectricalFaultType.PhaseLoss,
                    FaultSeverity.Danger,
                    $"Motor güç devresinde faz eksikliği algılandı. Aktif faz sayısı: {poweredPhaseCount}/3",
                    monitoredMotor
                )
            );
        }
    }

    private bool HasAnyPhase(
        ElectricalSimulationSnapshot snapshot,
        Terminal terminal)
    {
        if (terminal == null)
            return false;

        ElectricalPotentialFlags state =
            snapshot.GetPotential(
                terminal
            );

        ElectricalPotentialFlags phases =
            ElectricalPotentialFlags.PhaseL1 |
            ElectricalPotentialFlags.PhaseL2 |
            ElectricalPotentialFlags.PhaseL3;

        return (state & phases) != 0;
    }

    // =========================================
    // CONNECTION FAULTS
    // =========================================

    private void DetectConnectionFaults(
        List<ElectricalFault> faults)
    {
        if (connectionManager == null)
            return;

        IReadOnlyList<Connection> connections =
            connectionManager.Connections;

        for (
            int i = 0;
            i < connections.Count;
            i++)
        {
            Connection connection =
                connections[i];

            if (connection == null)
                continue;

            Terminal a =
                connection.TerminalA;

            Terminal b =
                connection.TerminalB;

            if (a == null ||
                b == null)
            {
                continue;
            }

            if (circuitDefinition != null &&
                !circuitDefinition
                    .IsRequiredConnection(
                        a.TerminalId,
                        b.TerminalId
                    ))
            {
                faults.Add(
                    new ElectricalFault(
                        ElectricalFaultType
                            .UnexpectedConnection,
                        FaultSeverity.Warning,
                        $"Beklenmeyen bağlantı: {a.DisplayName} ↔ {b.DisplayName}",
                        connection.Wire
                    )
                );
            }

            if (!AreLineTypesCompatible(
                    a.LineType,
                    b.LineType))
            {
                faults.Add(
                    new ElectricalFault(
                        ElectricalFaultType
                            .LineTypeMismatch,
                        FaultSeverity.Danger,
                        $"Hat tipi uyuşmazlığı: {a.DisplayName} ({a.LineType}) ↔ {b.DisplayName} ({b.LineType})",
                        connection.Wire
                    )
                );
            }
        }
    }

    private bool AreLineTypesCompatible(
        ElectricalLineType a,
        ElectricalLineType b)
    {
        if (a ==
            ElectricalLineType.Undefined)
        {
            return true;
        }

        if (b ==
            ElectricalLineType.Undefined)
        {
            return true;
        }

        return a == b;
    }

    // =========================================
    // RESULT
    // =========================================

    private void ReplaceFaults(
        List<ElectricalFault> faults)
    {
        activeFaults.Clear();

        activeFaults.AddRange(
            faults
        );

        FaultsChanged?.Invoke();
    }

    [ContextMenu("Print Active Faults")]
    public void PrintActiveFaults()
    {
        if (activeFaults.Count == 0)
        {
            Debug.Log(
                "FAULT SYSTEM | Aktif hata yok.",
                this
            );

            return;
        }

        foreach (
            ElectricalFault fault
            in activeFaults)
        {
            Debug.LogWarning(
                $"FAULT | " +
                $"{fault.Type} | " +
                $"{fault.Severity} | " +
                $"{fault.Message}",
                fault.Source
            );
        }
    }
}