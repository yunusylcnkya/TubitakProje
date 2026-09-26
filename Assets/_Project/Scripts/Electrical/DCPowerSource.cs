using UnityEngine;

public sealed class DCPowerSource :
    MonoBehaviour
{
    [Header("Owner")]
    [SerializeField]
    private Equipment owner;

    [Header("Terminals")]
    [SerializeField]
    private Terminal positiveTerminal;

    [SerializeField]
    private Terminal negativeTerminal;

    public Equipment Owner =>
        owner;

    public Terminal PositiveTerminal =>
        positiveTerminal;

    public Terminal NegativeTerminal =>
        negativeTerminal;

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