using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class AssessmentSystem : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField]
    private AssessmentConfig config;

    [Header("Systems")]
    [SerializeField]
    private CircuitValidator circuitValidator;

    [SerializeField]
    private ElectricalFaultSystem faultSystem;

    [SerializeField]
    private ElectricalSafetySystem safetySystem;

    [SerializeField]
    private WireRemovalController wireRemovalController;

    [Header("Runtime")]
    [SerializeField]
    private bool startAutomatically = true;

    private readonly HashSet<ElectricalFaultType>
        previousActiveFaultTypes =
            new HashSet<ElectricalFaultType>();

    private float startTime;

    private bool sessionRunning;
    private bool sessionFinished;

    private int previousIncorrectConnections;

    private int wrongConnectionAttempts;
    private int correctedWrongConnections;

    private int wireRemovals;
    private int safetyViolations;

    private int shortCircuits;
    private int phaseLosses;
    private int wrongPhaseSequences;
    private int openCircuits;
    private int thermalTrips;
    private int fuseBlows;

    public bool SessionRunning =>
        sessionRunning;

    public bool SessionFinished =>
        sessionFinished;

    public AssessmentResult LastResult
    {
        get;
        private set;
    }

    public event Action<AssessmentResult>
        AssessmentCompleted;

    // =========================================
    // UNITY EVENTS
    // =========================================

    private void OnEnable()
    {
        if (circuitValidator != null)
        {
            circuitValidator.ValidationChanged +=
                HandleValidationChanged;
        }

        if (faultSystem != null)
        {
            faultSystem.FaultsChanged +=
                HandleFaultsChanged;
        }

        if (safetySystem != null)
        {
            safetySystem.SafetyViolationRaised +=
                HandleSafetyViolation;
        }

        if (wireRemovalController != null)
        {
            wireRemovalController.WireRemoved +=
                HandleWireRemoved;
        }
    }

    private void Start()
    {
        if (startAutomatically)
        {
            BeginAssessment();
        }
    }

    private void OnDisable()
    {
        if (circuitValidator != null)
        {
            circuitValidator.ValidationChanged -=
                HandleValidationChanged;
        }

        if (faultSystem != null)
        {
            faultSystem.FaultsChanged -=
                HandleFaultsChanged;
        }

        if (safetySystem != null)
        {
            safetySystem.SafetyViolationRaised -=
                HandleSafetyViolation;
        }

        if (wireRemovalController != null)
        {
            wireRemovalController.WireRemoved -=
                HandleWireRemoved;
        }
    }

    // =========================================
    // SESSION
    // =========================================

    [ContextMenu("Begin Assessment")]
    public void BeginAssessment()
    {
        ResetCounters();

        sessionRunning = true;
        sessionFinished = false;

        startTime =
            Time.time;

        // CircuitValidationResult struct olduğu için
        // null kontrolü yapmıyoruz.
        //
        // Ayrıca AssessmentSystem ve CircuitValidator
        // Start sırası garanti olmadığı için
        // burada güncel validation'ı özellikle istiyoruz.
        if (circuitValidator != null)
        {
            circuitValidator.Validate();

            previousIncorrectConnections =
                circuitValidator
                    .LastResult
                    .IncorrectConnections;
        }
        else
        {
            previousIncorrectConnections = 0;
        }

        CaptureCurrentFaultTypes();

        Debug.Log(
            "ASSESSMENT | Oturum başladı.",
            this
        );
    }

    [ContextMenu("Finish Assessment")]
    public void FinishAssessment()
    {
        if (!sessionRunning ||
            sessionFinished)
        {
            return;
        }

        // Final sonucu almadan hemen önce
        // circuit state'i tekrar doğrula.
        if (circuitValidator != null)
        {
            circuitValidator.Validate();
        }

        float duration =
            Time.time - startTime;

        bool circuitValid =
            circuitValidator != null &&
            circuitValidator.LastResult.IsValid;

        int score =
            CalculateScore(
                circuitValid
            );

        bool passed =
            circuitValid &&
            config != null &&
            score >= config.PassScore;

        LastResult =
            new AssessmentResult(
                score,
                passed,
                circuitValid,
                duration,
                wrongConnectionAttempts,
                correctedWrongConnections,
                wireRemovals,
                safetyViolations,
                shortCircuits,
                phaseLosses,
                wrongPhaseSequences,
                openCircuits,
                thermalTrips,
                fuseBlows
            );

        sessionRunning = false;
        sessionFinished = true;

        Debug.Log(
            $"ASSESSMENT COMPLETE | " +
            $"Score: {LastResult.Score}/100 | " +
            $"Passed: {LastResult.Passed} | " +
            $"CircuitValid: {LastResult.CircuitValid} | " +
            $"Time: {LastResult.GetFormattedDuration()}",
            this
        );

        AssessmentCompleted?.Invoke(
            LastResult
        );
    }

    [ContextMenu("Reset Assessment")]
    public void ResetAssessment()
    {
        BeginAssessment();
    }

    // =========================================
    // CIRCUIT VALIDATION
    // =========================================

    private void HandleValidationChanged(
        CircuitValidationResult result)
    {
        if (!sessionRunning)
            return;

        int currentIncorrect =
            result.IncorrectConnections;

        if (currentIncorrect >
            previousIncorrectConnections)
        {
            int difference =
                currentIncorrect -
                previousIncorrectConnections;

            wrongConnectionAttempts +=
                difference;
        }
        else if (currentIncorrect <
                 previousIncorrectConnections)
        {
            int difference =
                previousIncorrectConnections -
                currentIncorrect;

            correctedWrongConnections +=
                difference;
        }

        previousIncorrectConnections =
            currentIncorrect;
    }

    // =========================================
    // FAULTS
    // =========================================

    private void HandleFaultsChanged()
    {
        if (!sessionRunning ||
            faultSystem == null)
        {
            return;
        }

        HashSet<ElectricalFaultType>
            currentTypes =
                new HashSet<ElectricalFaultType>();

        IReadOnlyList<ElectricalFault>
            faults =
                faultSystem.ActiveFaults;

        for (
            int i = 0;
            i < faults.Count;
            i++)
        {
            ElectricalFault fault =
                faults[i];

            if (fault == null)
                continue;

            currentTypes.Add(
                fault.Type
            );

            if (!previousActiveFaultTypes.Contains(
                    fault.Type))
            {
                RegisterFaultActivation(
                    fault.Type
                );
            }
        }

        previousActiveFaultTypes.Clear();

        foreach (
            ElectricalFaultType type
            in currentTypes)
        {
            previousActiveFaultTypes.Add(
                type
            );
        }
    }

    private void RegisterFaultActivation(
        ElectricalFaultType type)
    {
        switch (type)
        {
            case ElectricalFaultType.ShortCircuit:
                shortCircuits++;
                break;

            case ElectricalFaultType.PhaseLoss:
                phaseLosses++;
                break;

            case ElectricalFaultType.WrongPhaseSequence:
                wrongPhaseSequences++;
                break;

            case ElectricalFaultType.OpenCircuit:
                openCircuits++;
                break;

            case ElectricalFaultType.ThermalTrip:
                thermalTrips++;
                break;

            case ElectricalFaultType.FuseBlown:
                fuseBlows++;
                break;
        }
    }

    private void CaptureCurrentFaultTypes()
    {
        previousActiveFaultTypes.Clear();

        if (faultSystem == null)
            return;

        IReadOnlyList<ElectricalFault>
            faults =
                faultSystem.ActiveFaults;

        for (
            int i = 0;
            i < faults.Count;
            i++)
        {
            ElectricalFault fault =
                faults[i];

            if (fault == null)
                continue;

            previousActiveFaultTypes.Add(
                fault.Type
            );
        }
    }

    // =========================================
    // SAFETY
    // =========================================

    private void HandleSafetyViolation(
        string message)
    {
        if (!sessionRunning)
            return;

        safetyViolations++;

        Debug.LogWarning(
            $"ASSESSMENT | " +
            $"Safety violation #{safetyViolations}",
            this
        );
    }

    // =========================================
    // WIRE REMOVAL
    // =========================================

    private void HandleWireRemoved(
        Wire wire)
    {
        if (!sessionRunning)
            return;

        wireRemovals++;
    }

    // =========================================
    // SCORE
    // =========================================

    private int CalculateScore(
        bool circuitValid)
    {
        if (config == null)
        {
            Debug.LogError(
                "ASSESSMENT | AssessmentConfig atanmadı.",
                this
            );

            return 0;
        }

        int score =
            config.StartingScore;

        score -=
            wrongConnectionAttempts *
            config.WrongConnectionPenalty;

        score -=
            wireRemovals *
            config.WireRemovalPenalty;

        score -=
            safetyViolations *
            config.SafetyViolationPenalty;

        score -=
            shortCircuits *
            config.ShortCircuitPenalty;

        score -=
            phaseLosses *
            config.PhaseLossPenalty;

        score -=
            wrongPhaseSequences *
            config.WrongPhaseSequencePenalty;

        score -=
            openCircuits *
            config.OpenCircuitPenalty;

        score -=
            thermalTrips *
            config.ThermalTripPenalty;

        score -=
            fuseBlows *
            config.FuseBlownPenalty;

        score =
            Mathf.Clamp(
                score,
                0,
                100
            );

        if (!circuitValid)
        {
            score =
                Mathf.Min(
                    score,
                    config.IncompleteCircuitMaxScore
                );
        }

        return score;
    }

    // =========================================
    // RESET
    // =========================================

    private void ResetCounters()
    {
        LastResult = null;

        previousIncorrectConnections = 0;

        wrongConnectionAttempts = 0;
        correctedWrongConnections = 0;

        wireRemovals = 0;
        safetyViolations = 0;

        shortCircuits = 0;
        phaseLosses = 0;
        wrongPhaseSequences = 0;
        openCircuits = 0;
        thermalTrips = 0;
        fuseBlows = 0;

        previousActiveFaultTypes.Clear();
    }
}