using UnityEngine;

public sealed class ThreePhasePowerSource :
    MonoBehaviour
{
    [Header("Owner")]
    [SerializeField]
    private Equipment owner;

    [Header("Phase Terminals")]
    [SerializeField]
    private Terminal phaseL1Terminal;

    [SerializeField]
    private Terminal phaseL2Terminal;

    [SerializeField]
    private Terminal phaseL3Terminal;

    public Equipment Owner =>
        owner;

    public Terminal PhaseL1Terminal =>
        phaseL1Terminal;

    public Terminal PhaseL2Terminal =>
        phaseL2Terminal;

    public Terminal PhaseL3Terminal =>
        phaseL3Terminal;

    public bool IsActive =>
        owner != null &&
        owner.CurrentState ==
            EquipmentState.Placed;

    private void Awake()
    {
        if (owner == null)
        {
            owner =
                GetComponent<Equipment>();
        }
    }
}