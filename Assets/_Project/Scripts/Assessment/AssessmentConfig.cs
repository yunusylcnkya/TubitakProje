using UnityEngine;

[CreateAssetMenu(
    fileName = "AC_Assessment",
    menuName = "Industrial Automation/Assessment/Assessment Config"
)]
public sealed class AssessmentConfig : ScriptableObject
{
    [Header("General")]
    [SerializeField, Range(0, 100)]
    private int startingScore = 100;

    [SerializeField, Range(0, 100)]
    private int passScore = 70;

    [SerializeField, Range(0, 100)]
    private int incompleteCircuitMaxScore = 49;

    [Header("Connection Penalties")]
    [SerializeField, Min(0)]
    private int wrongConnectionPenalty = 3;

    [SerializeField, Min(0)]
    private int wireRemovalPenalty = 0;

    [Header("Electrical Fault Penalties")]
    [SerializeField, Min(0)]
    private int shortCircuitPenalty = 15;

    [SerializeField, Min(0)]
    private int phaseLossPenalty = 8;

    [SerializeField, Min(0)]
    private int wrongPhaseSequencePenalty = 5;

    [SerializeField, Min(0)]
    private int openCircuitPenalty = 5;

    [SerializeField, Min(0)]
    private int thermalTripPenalty = 8;

    [SerializeField, Min(0)]
    private int fuseBlownPenalty = 5;

    [Header("Safety")]
    [SerializeField, Min(0)]
    private int safetyViolationPenalty = 10;

    public int StartingScore => startingScore;
    public int PassScore => passScore;
    public int IncompleteCircuitMaxScore => incompleteCircuitMaxScore;

    public int WrongConnectionPenalty => wrongConnectionPenalty;
    public int WireRemovalPenalty => wireRemovalPenalty;

    public int ShortCircuitPenalty => shortCircuitPenalty;
    public int PhaseLossPenalty => phaseLossPenalty;
    public int WrongPhaseSequencePenalty => wrongPhaseSequencePenalty;
    public int OpenCircuitPenalty => openCircuitPenalty;
    public int ThermalTripPenalty => thermalTripPenalty;
    public int FuseBlownPenalty => fuseBlownPenalty;

    public int SafetyViolationPenalty => safetyViolationPenalty;
}