using System;
using UnityEngine;

public sealed class DCPowerSource : MonoBehaviour, IPlacedEquipmentControl
{
    [Header("Owner")]
    [SerializeField] private Equipment owner;

    [Header("DC Output Terminals")]
    [SerializeField] private Terminal positiveTerminal;
    [SerializeField] private Terminal negativeTerminal;

    [Header("AC Input Terminals")]
    [SerializeField] private Terminal phaseInputTerminal;
    [SerializeField] private Terminal neutralInputTerminal;
    [SerializeField] private Terminal protectiveEarthTerminal;

    [Header("Power Switch")]
    [SerializeField] private bool switchEnabled = false;

    [Header("Status Light - existing Point Light")]
    [SerializeField] private Light statusLight;
    [SerializeField] private Color activeColor = Color.green;
    [SerializeField] private Color inactiveColor = Color.red;

    [Header("Status LED Lens - assign a small mesh on the PSU face")]
    [SerializeField] private Renderer statusLensRenderer;
    [SerializeField] private Color inactiveLensColor = new Color(0.04f, 0.09f, 0.05f, 1f);
    [SerializeField, Min(0f)] private float activeLensEmission = 2.5f;

    [Header("Status LED Light Settings")]
    [SerializeField, Min(0f)] private float activeLightIntensity = 0.65f;
    [SerializeField, Min(0.01f)] private float activeLightRange = 0.12f;
    [Tooltip("Keep disabled for a dark indicator when PSU is not active. Enable to retain the original red-standby behavior.")]
    [SerializeField] private bool illuminateWhenInactive = false;
    [SerializeField, Min(0f)] private float inactiveLightIntensity = 0.1f;

    private bool inputPowered;
    private Material runtimeLensMaterial;
    private bool visualStateInitialized;
    private bool previousActive;
    private bool previousPlaced;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    public event Action StateChanged;

    public Equipment Owner => owner;
    public Terminal PositiveTerminal => positiveTerminal;
    public Terminal NegativeTerminal => negativeTerminal;
    public Terminal PhaseInputTerminal => phaseInputTerminal;
    public Terminal NeutralInputTerminal => neutralInputTerminal;
    public Terminal ProtectiveEarthTerminal => protectiveEarthTerminal;
    public bool IsSwitchedOn => switchEnabled;
    public bool HasValidACInput => inputPowered;

    public bool IsActive =>
        isActiveAndEnabled &&
        owner != null &&
        owner.CurrentState == EquipmentState.Placed &&
        switchEnabled &&
        inputPowered;

    private void Awake()
    {
        if (owner == null)
            owner = GetComponent<Equipment>();

        if (statusLensRenderer != null)
        {
            // The runtime instance is unique to this PSU. The source material asset
            // (and the materials used by other equipment) are not modified.
            runtimeLensMaterial = statusLensRenderer.material;
            if (runtimeLensMaterial != null &&
                runtimeLensMaterial.HasProperty(EmissionColorId))
                runtimeLensMaterial.EnableKeyword("_EMISSION");
        }

        RefreshStatusLight();
    }

    private void OnEnable()
    {
        RefreshStatusLight();
    }

    private void Update()
    {
        // Placement changes do not necessarily trigger DCPowerSource.StateChanged.
        // Rebuild the visuals only if the actual PSU status has changed.
        bool placed = owner != null && owner.CurrentState == EquipmentState.Placed;
        bool active = IsActive;
        if (!visualStateInitialized || placed != previousPlaced || active != previousActive)
            RefreshStatusLight();
    }

    private void OnDisable()
    {
        if (statusLight != null)
            statusLight.enabled = false;
        if (runtimeLensMaterial != null)
            ApplyLensColor(inactiveLensColor, Color.black);
        visualStateInitialized = false;
    }

    private void OnDestroy()
    {
        if (runtimeLensMaterial != null)
        {
            Destroy(runtimeLensMaterial);
            runtimeLensMaterial = null;
        }
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

        // Keep the original solver event and power-switch behavior unchanged.
        StateChanged?.Invoke();
    }

    public string GetActionText(InteractionContext context)
    {
        if (!switchEnabled)
            return inputPowered
                ? "[E] Devreye Enerji Ver"
                : "[E] Aç (L/N beslemesi yok)";

        if (!inputPowered)
            return "[E] Kapat (L/N beslemesi yok)";

        return "[E] Enerjiyi Kes";
    }

    public void EvaluateInputPower(ElectricalSimulationSnapshot acSnapshot)
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
            bool hasPhase = acSnapshot.HasPotential(
                phaseInputTerminal, ElectricalPotentialFlags.PhaseL1);
            bool hasNeutral = acSnapshot.HasPotential(
                neutralInputTerminal, ElectricalPotentialFlags.Neutral);
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
        bool placed = owner != null && owner.CurrentState == EquipmentState.Placed;
        bool active = IsActive;

        if (statusLight != null)
        {
            statusLight.color = active ? activeColor : inactiveColor;
            statusLight.intensity = active ? activeLightIntensity : inactiveLightIntensity;
            statusLight.range = activeLightRange;
            statusLight.enabled = placed && (active || illuminateWhenInactive);
        }

        if (runtimeLensMaterial != null)
        {
            Color lensColor = active ? activeColor : inactiveLensColor;
            Color emissionColor = active
                ? activeColor * activeLensEmission
                : Color.black;
            ApplyLensColor(lensColor, emissionColor);
        }

        previousPlaced = placed;
        previousActive = active;
        visualStateInitialized = true;
    }

    private void ApplyLensColor(Color baseColor, Color emissionColor)
    {
        if (runtimeLensMaterial == null)
            return;

        if (runtimeLensMaterial.HasProperty(BaseColorId))
            runtimeLensMaterial.SetColor(BaseColorId, baseColor);
        else if (runtimeLensMaterial.HasProperty(ColorId))
            runtimeLensMaterial.SetColor(ColorId, baseColor);

        if (runtimeLensMaterial.HasProperty(EmissionColorId))
            runtimeLensMaterial.SetColor(EmissionColorId, emissionColor);
    }
}
