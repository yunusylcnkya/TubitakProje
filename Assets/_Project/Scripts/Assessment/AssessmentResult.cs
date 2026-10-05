using System;

public sealed class AssessmentResult
{
    public int Score { get; }

    public bool Passed { get; }

    public bool CircuitValid { get; }

    public float DurationSeconds { get; }

    public int WrongConnectionAttempts { get; }

    public int CorrectedWrongConnections { get; }

    public int WireRemovals { get; }

    public int SafetyViolations { get; }

    public int ShortCircuits { get; }

    public int PhaseLosses { get; }

    public int WrongPhaseSequences { get; }

    public int OpenCircuits { get; }

    public int ThermalTrips { get; }

    public int FuseBlows { get; }

    public AssessmentResult(
        int score,
        bool passed,
        bool circuitValid,
        float durationSeconds,
        int wrongConnectionAttempts,
        int correctedWrongConnections,
        int wireRemovals,
        int safetyViolations,
        int shortCircuits,
        int phaseLosses,
        int wrongPhaseSequences,
        int openCircuits,
        int thermalTrips,
        int fuseBlows)
    {
        Score = score;
        Passed = passed;
        CircuitValid = circuitValid;
        DurationSeconds = durationSeconds;

        WrongConnectionAttempts =
            wrongConnectionAttempts;

        CorrectedWrongConnections =
            correctedWrongConnections;

        WireRemovals = wireRemovals;
        SafetyViolations = safetyViolations;

        ShortCircuits = shortCircuits;
        PhaseLosses = phaseLosses;
        WrongPhaseSequences = wrongPhaseSequences;
        OpenCircuits = openCircuits;
        ThermalTrips = thermalTrips;
        FuseBlows = fuseBlows;
    }

    public string GetFormattedDuration()
    {
        TimeSpan duration =
            TimeSpan.FromSeconds(
                DurationSeconds
            );

        return
            $"{(int)duration.TotalMinutes:00}:" +
            $"{duration.Seconds:00}";
    }
}