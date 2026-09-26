using UnityEngine;

public sealed class SignalLampBehaviour :
    MonoBehaviour
{
    [Header("Electrical")]
    [SerializeField]
    private ElectricalSimulationSystem
        electricalSimulation;

    [SerializeField]
    private Terminal positiveTerminal;

    [SerializeField]
    private Terminal negativeTerminal;

    [SerializeField]
    private bool polaritySensitive = true;

    [Header("Visual")]
    [SerializeField]
    private Renderer lampRenderer;

    [SerializeField]
    private Color offColor =
        Color.black;

    [SerializeField]
    private Color onColor =
        Color.green;

    private Material runtimeMaterial;

    public bool IsOn { get; private set; }

    private void Awake()
    {
        if (lampRenderer != null)
        {
            runtimeMaterial =
                lampRenderer.material;
        }
    }

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
        RefreshFromCurrentSimulation();
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
        ApplyElectricalState(snapshot);
    }

    private void RefreshFromCurrentSimulation()
    {
        if (electricalSimulation == null)
        {
            TurnOff();
            return;
        }

        ApplyElectricalState(
            electricalSimulation.LastSnapshot
        );
    }

    private void ApplyElectricalState(
        ElectricalSimulationSnapshot snapshot)
    {
        if (snapshot == null)
        {
            TurnOff();
            return;
        }

        if (snapshot.HasShortCircuit)
        {
            TurnOff();
            return;
        }

        bool powered;

        if (polaritySensitive)
        {
            powered =
                snapshot.HasPotential(
                    positiveTerminal,
                    ElectricalPotentialFlags
                        .DCPositive
                )
                &&
                snapshot.HasPotential(
                    negativeTerminal,
                    ElectricalPotentialFlags
                        .DCNegative
                );
        }
        else
        {
            bool normal =
                snapshot.HasPotential(
                    positiveTerminal,
                    ElectricalPotentialFlags
                        .DCPositive
                )
                &&
                snapshot.HasPotential(
                    negativeTerminal,
                    ElectricalPotentialFlags
                        .DCNegative
                );

            bool reversed =
                snapshot.HasPotential(
                    positiveTerminal,
                    ElectricalPotentialFlags
                        .DCNegative
                )
                &&
                snapshot.HasPotential(
                    negativeTerminal,
                    ElectricalPotentialFlags
                        .DCPositive
                );

            powered =
                normal || reversed;
        }

        if (powered)
        {
            TurnOn();
        }
        else
        {
            TurnOff();
        }
    }

    private void TurnOn()
    {
        IsOn = true;

        if (runtimeMaterial != null)
        {
            runtimeMaterial.color =
                onColor;
        }
    }

    private void TurnOff()
    {
        IsOn = false;

        if (runtimeMaterial != null)
        {
            runtimeMaterial.color =
                offColor;
        }
    }
}