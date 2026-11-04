
using System;
using UnityEngine;

public sealed class DCPowerSource :
    MonoBehaviour,
    IPlacedEquipmentControl
{
    [Header("Owner")]
    [SerializeField]
    private Equipment owner;

    [Header("DC Output Terminals")]
    [SerializeField]
    private Terminal positiveTerminal;

    [SerializeField]
    private Terminal negativeTerminal;

    [Header("AC Input Terminals")]
    [SerializeField]
    private Terminal phaseInputTerminal;

    [SerializeField]
    private Terminal neutralInputTerminal;

    [SerializeField]
    private Terminal protectiveEarthTerminal;

    [Header("Power Switch")]
    [SerializeField]
    private bool switchEnabled = false;

    [Header("Status Light")]
    [SerializeField]
    private Light statusLight;

    [SerializeField]
    private Color activeColor = Color.green;

    [SerializeField]
    private Color inactiveColor = Color.red;

    private bool inputPowered;

    public event Action StateChanged;

    public Equipment Owner => owner;

    public Terminal PositiveTerminal =>
        positiveTerminal;

    public Terminal NegativeTerminal =>
        negativeTerminal;

    public Terminal PhaseInputTerminal =>
        phaseInputTerminal;

    public Terminal NeutralInputTerminal =>
        neutralInputTerminal;

    public Terminal ProtectiveEarthTerminal =>
        protectiveEarthTerminal;

    public bool IsSwitchedOn =>
        switchEnabled;

    public bool HasValidACInput =>
        inputPowered;

    public bool IsActive =>
        isActiveAndEnabled &&
        owner != null &&
        owner.CurrentState == EquipmentState.Placed &&
        switchEnabled &&
        inputPowered;

    private void Awake()
    {
        if (owner == null)
        {
            owner = GetComponent<Equipment>();
        }

        RefreshStatusLight();
    }

    public bool CanUse(InteractionContext context)
    {
        return isActiveAndEnabled &&
               owner != null &&
               owner.CurrentState == EquipmentState.Placed;
    }

    public void Use(InteractionContext context)
    {
        if (!CanUse(context))
            return;

        switchEnabled = !switchEnabled;

        RefreshStatusLight();

        Debug.Log(
            $"POWER SUPPLY | {name} | " +
            $"Switch: {(switchEnabled ? "ON" : "OFF")}",
            this
        );

        // Solver yeniden hesaplama yapacak.
        StateChanged?.Invoke();
    }

    public string GetActionText(InteractionContext context)
    {
        if (!switchEnabled)
        {
            return inputPowered
                ? "[E] Devreye Enerji Ver"
                : "[E] Aç (L/N beslemesi yok)";
        }

        if (!inputPowered)
        {
            return "[E] Kapat (L/N beslemesi yok)";
        }

        return "[E] Enerjiyi Kes";
    }

    // Bu metot yalnızca elektriksel solver tarafından
    // AC girişleri değerlendirildikten sonra çağrılır.
    public void EvaluateInputPower(
        ElectricalSimulationSnapshot acSnapshot)
    {
        bool validInput = false;

        if (isActiveAndEnabled &&
            owner != null &&
            owner.CurrentState == EquipmentState.Placed &&
            phaseInputTerminal != null &&
            neutralInputTerminal != null &&
            acSnapshot != null &&
            !acSnapshot.HasShortCircuit)
        {
            bool hasPhase =
                acSnapshot.HasPotential(
                    phaseInputTerminal,
                    ElectricalPotentialFlags.PhaseL1
                );

            bool hasNeutral =
                acSnapshot.HasPotential(
                    neutralInputTerminal,
                    ElectricalPotentialFlags.Neutral
                );

            validInput = hasPhase && hasNeutral;
        }

        if (inputPowered != validInput)
        {
            inputPowered = validInput;

            Debug.Log(
                $"POWER SUPPLY | {name} | " +
                $"AC Input: {(inputPowered ? "VALID" : "INVALID")}",
                this
            );
        }

        RefreshStatusLight();
    }

    private void RefreshStatusLight()
    {
        if (statusLight == null)
            return;

        bool isPlaced =
            owner != null &&
            owner.CurrentState == EquipmentState.Placed;

        statusLight.enabled = isPlaced;

        statusLight.color =
            IsActive ? activeColor : inactiveColor;
    }
}
