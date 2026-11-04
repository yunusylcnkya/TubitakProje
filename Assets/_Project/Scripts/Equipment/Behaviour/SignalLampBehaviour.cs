
using UnityEngine;

public sealed class SignalLampBehaviour : MonoBehaviour
{
    [Header("Electrical")]
    [SerializeField]
    private ElectricalSimulationSystem electricalSimulation;

    [SerializeField]
    private Terminal positiveTerminal;

    [SerializeField]
    private Terminal negativeTerminal;

    [SerializeField]
    private bool polaritySensitive = true;

    [Header("Lamp Visual")]
    [SerializeField]
    private Renderer lampRenderer;

    [SerializeField]
    private Color offColor = Color.black;

    [SerializeField]
    private Color onColor = Color.green;

    [Header("Emission")]
    [SerializeField, Min(0f)]
    private float emissionIntensity = 3f;

    [Header("Real Light")]
    [SerializeField]
    private Light pointLight;

    [SerializeField, Min(0f)]
    private float lightIntensity = 1.5f;

    [SerializeField, Min(0.01f)]
    private float lightRange = 0.5f;

    private Material runtimeMaterial;

    private static readonly int BaseColorId =
        Shader.PropertyToID("_BaseColor");

    private static readonly int ColorId =
        Shader.PropertyToID("_Color");

    private static readonly int EmissionColorId =
        Shader.PropertyToID("_EmissionColor");

    public bool IsOn { get; private set; }

    // =========================================
    // UNITY LIFECYCLE
    // =========================================

    private void Awake()
    {
        if (lampRenderer != null)
        {
            runtimeMaterial = lampRenderer.material;

            if (runtimeMaterial != null &&
                runtimeMaterial.HasProperty(EmissionColorId))
            {
                runtimeMaterial.EnableKeyword("_EMISSION");
            }
        }

        IsOn = false;
        ApplyVisualState();
    }

    private void OnEnable()
    {
        if (electricalSimulation != null)
        {
            electricalSimulation.SimulationChanged +=
                HandleSimulationChanged;
        }

        RefreshFromCurrentSimulation();
    }

    private void Start()
    {
        RefreshFromCurrentSimulation();
    }

    private void OnDisable()
    {
        if (electricalSimulation != null)
        {
            electricalSimulation.SimulationChanged -=
                HandleSimulationChanged;
        }

        SetLampState(false);
    }

    private void OnDestroy()
    {
        if (runtimeMaterial != null)
        {
            Destroy(runtimeMaterial);
            runtimeMaterial = null;
        }
    }

    // =========================================
    // ELECTRICAL SIMULATION
    // =========================================

    private void HandleSimulationChanged(
        ElectricalSimulationSnapshot snapshot)
    {
        ApplyElectricalState(snapshot);
    }

    private void RefreshFromCurrentSimulation()
    {
        if (electricalSimulation == null)
        {
            SetLampState(false);
            return;
        }

        ApplyElectricalState(
            electricalSimulation.LastSnapshot
        );
    }

    private void ApplyElectricalState(
        ElectricalSimulationSnapshot snapshot)
    {
        if (snapshot == null ||
            snapshot.HasShortCircuit ||
            positiveTerminal == null ||
            negativeTerminal == null ||
            positiveTerminal == negativeTerminal)
        {
            SetLampState(false);
            return;
        }

        bool normalPolarity =
            snapshot.HasPotential(
                positiveTerminal,
                ElectricalPotentialFlags.DCPositive
            )
            &&
            snapshot.HasPotential(
                negativeTerminal,
                ElectricalPotentialFlags.DCNegative
            );

        bool powered = normalPolarity;

        if (!polaritySensitive)
        {
            bool reversedPolarity =
                snapshot.HasPotential(
                    positiveTerminal,
                    ElectricalPotentialFlags.DCNegative
                )
                &&
                snapshot.HasPotential(
                    negativeTerminal,
                    ElectricalPotentialFlags.DCPositive
                );

            powered =
                normalPolarity || reversedPolarity;
        }

        SetLampState(powered);
    }

    // =========================================
    // STATE CONTROL
    // =========================================

    private void SetLampState(bool shouldBeOn)
    {
        if (IsOn == shouldBeOn)
            return;

        IsOn = shouldBeOn;

        ApplyVisualState();
    }

    // =========================================
    // VISUAL STATE
    // =========================================

    private void ApplyVisualState()
    {
        Color targetColor =
            IsOn ? onColor : offColor;

        UpdateMaterial(targetColor);

        UpdatePointLight();
    }

    private void UpdateMaterial(Color targetColor)
    {
        if (runtimeMaterial == null)
            return;

        // URP/Lit uses _BaseColor.
        if (runtimeMaterial.HasProperty(BaseColorId))
        {
            runtimeMaterial.SetColor(
                BaseColorId,
                targetColor
            );
        }
        else if (runtimeMaterial.HasProperty(ColorId))
        {
            runtimeMaterial.SetColor(
                ColorId,
                targetColor
            );
        }

        // HDR emission.
        if (runtimeMaterial.HasProperty(EmissionColorId))
        {
            Color emissionColor =
                IsOn
                    ? onColor * emissionIntensity
                    : Color.black;

            runtimeMaterial.SetColor(
                EmissionColorId,
                emissionColor
            );
        }
    }

    private void UpdatePointLight()
    {
        if (pointLight == null)
            return;

        pointLight.color = onColor;

        pointLight.intensity = lightIntensity;

        pointLight.range = lightRange;

        pointLight.enabled = IsOn;
    }
}
