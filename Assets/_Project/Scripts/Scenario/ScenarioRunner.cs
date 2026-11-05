using System;
using UnityEngine;

public sealed class ScenarioRunner : MonoBehaviour
{
    [Header("Scenario")]
    [SerializeField] private ScenarioDefinition scenario;

    [Header("Systems")]
    [SerializeField] private PlacementManager placementManager;
    [SerializeField] private ConnectionManager connectionManager;
    [SerializeField] private CircuitValidator circuitValidator;

    [Header("Behaviour")]
    [SerializeField] private bool lockAfterCompletion = true;

    // Optional: only assign these in scenarios containing FunctionalState.
    // Other scenarios remain compatible with the original three task types.
    [Header("Optional functional verification (v19.20.8.2)")]
    [SerializeField] private DCPowerSource functionalPowerSupply;
    [SerializeField] private FuseDevice functionalFuse;
    [SerializeField] private SignalLampBehaviour functionalLamp;
    [SerializeField] private ElectricalSimulationSystem functionalSimulation;

    private int currentStepIndex;
    private bool isCompleted;
    private bool[] functionalPassed = Array.Empty<bool>();
    private bool functionalRefreshPending;

    public event Action ProgressChanged;
    public event Action ScenarioCompleted;

    public ScenarioDefinition Scenario => scenario;
    public int CurrentStepIndex => currentStepIndex;
    public bool IsCompleted => isCompleted;
    public int TotalSteps => scenario != null && scenario.Steps != null
        ? scenario.Steps.Length : 0;
    public int CompletedStepCount => isCompleted ? TotalSteps : currentStepIndex;

    public TaskStepDefinition CurrentStep
    {
        get
        {
            if (scenario == null || scenario.Steps == null ||
                currentStepIndex < 0 || currentStepIndex >= scenario.Steps.Length)
                return null;

            return scenario.Steps[currentStepIndex];
        }
    }

    private void OnEnable()
    {
        if (placementManager != null)
            placementManager.PlacementsChanged += HandleStateChanged;
        if (connectionManager != null)
            connectionManager.ConnectionsChanged += HandleStateChanged;
        if (circuitValidator != null)
            circuitValidator.ValidationChanged += HandleValidationChanged;

        if (functionalPowerSupply != null)
            functionalPowerSupply.StateChanged += MarkFunctionalRefresh;
        if (functionalFuse != null)
            functionalFuse.StateChanged += MarkFunctionalRefresh;
        if (functionalSimulation != null)
            functionalSimulation.SimulationChanged += HandleSimulationChanged;
    }

    private void Start()
    {
        StartScenario();
    }

    // Functional state is checked AFTER the simulation and lamp visual state
    // have had a chance to process their events in this frame. No per-frame
    // scanning occurs when electrical state has not changed.
    private void LateUpdate()
    {
        if (!functionalRefreshPending)
            return;

        functionalRefreshPending = false;
        RecalculateProgress(false);
    }

    private void OnDisable()
    {
        if (placementManager != null)
            placementManager.PlacementsChanged -= HandleStateChanged;
        if (connectionManager != null)
            connectionManager.ConnectionsChanged -= HandleStateChanged;
        if (circuitValidator != null)
            circuitValidator.ValidationChanged -= HandleValidationChanged;

        if (functionalPowerSupply != null)
            functionalPowerSupply.StateChanged -= MarkFunctionalRefresh;
        if (functionalFuse != null)
            functionalFuse.StateChanged -= MarkFunctionalRefresh;
        if (functionalSimulation != null)
            functionalSimulation.SimulationChanged -= HandleSimulationChanged;
    }

    public void StartScenario()
    {
        currentStepIndex = 0;
        isCompleted = false;
        functionalPassed = new bool[TotalSteps];
        functionalRefreshPending = false;

        Debug.Log($"Senaryo başladı: {scenario?.DisplayName}");
        RecalculateProgress(true);
    }

    private void HandleStateChanged()
    {
        RecalculateProgress(false);
    }

    private void HandleSimulationChanged(ElectricalSimulationSnapshot snapshot)
    {
        MarkFunctionalRefresh();
    }

    private void MarkFunctionalRefresh()
    {
        functionalRefreshPending = true;
    }

    private void HandleValidationChanged(CircuitValidationResult result)
    {
        RecalculateProgress(false);
    }

    public bool IsStepCompleted(int index)
    {
        if (scenario == null || scenario.Steps == null ||
            index < 0 || index >= scenario.Steps.Length)
            return false;

        TaskStepDefinition step = scenario.Steps[index];
        if (step == null)
            return false;

        // Don't show future tests as completed just because their conditions
        // happen to match the current state. They must be reached in order.
        if (step.StepType == TaskStepType.FunctionalState)
            return index < functionalPassed.Length && functionalPassed[index];

        return IsStepSatisfied(step);
    }

    private void RecalculateProgress(bool forceNotify)
    {
        if (scenario == null || scenario.Steps == null || scenario.Steps.Length == 0)
            return;
        if (isCompleted && lockAfterCompletion)
            return;

        int previousIndex = currentStepIndex;
        int newIndex = FindFirstIncompleteStep();
        bool scenarioNowComplete = newIndex >= scenario.Steps.Length;

        if (scenarioNowComplete)
        {
            currentStepIndex = scenario.Steps.Length;
            if (!isCompleted)
            {
                isCompleted = true;
                Debug.Log("SENARYO TAMAMLANDI ✓");
                ProgressChanged?.Invoke();
                ScenarioCompleted?.Invoke();
            }
            return;
        }

        isCompleted = false;
        currentStepIndex = newIndex;
        if (forceNotify || previousIndex != currentStepIndex)
        {
            Debug.Log($"Yeni görev: {CurrentStep.Instruction}");
            ProgressChanged?.Invoke();
        }
    }

    private int FindFirstIncompleteStep()
    {
        TaskStepDefinition[] steps = scenario.Steps;
        for (int i = 0; i < steps.Length; i++)
        {
            TaskStepDefinition step = steps[i];
            if (step == null)
                return i;

            if (step.StepType == TaskStepType.FunctionalState)
            {
                // Functional steps are sequential and latched: opening F1
                // later must not invalidate an earlier successful lamp-ON test.
                if (i >= functionalPassed.Length)
                    return i;
                if (functionalPassed[i])
                    continue;
                if (!IsStepSatisfied(step))
                    return i;
                functionalPassed[i] = true;
                continue;
            }

            // Physical placement/wiring/validation are always checked live,
            // preserving the old behavior when a wire is removed.
            if (!IsStepSatisfied(step))
                return i;
        }
        return steps.Length;
    }

    private bool IsStepSatisfied(TaskStepDefinition step)
    {
        if (step == null)
            return false;

        switch (step.StepType)
        {
            case TaskStepType.EquipmentPlaced:
                return placementManager != null &&
                    placementManager.IsEquipmentPlaced(step.Equipment, step.SlotId);

            case TaskStepType.ConnectionCreated:
                return connectionManager != null &&
                    connectionManager.HasConnection(step.TerminalIdA, step.TerminalIdB);

            case TaskStepType.CircuitValid:
                return circuitValidator != null && circuitValidator.LastResult.IsValid;

            case TaskStepType.FunctionalState:
                return IsFunctionalStateSatisfied(step);

            default:
                return false;
        }
    }

    private bool IsFunctionalStateSatisfied(TaskStepDefinition step)
    {
        // Reject a FunctionalState step with all conditions left at Ignore.
        if (step.PowerSupplyState == ExpectedSwitchState.Ignore &&
            step.FuseState == ExpectedSwitchState.Ignore &&
            step.LampState == ExpectedSwitchState.Ignore)
            return false;

        // Never pass a functional test when a short circuit is present.
        if (functionalSimulation == null || functionalSimulation.LastSnapshot == null ||
            functionalSimulation.LastSnapshot.HasShortCircuit)
            return false;

        if (step.PowerSupplyState != ExpectedSwitchState.Ignore)
        {
            if (functionalPowerSupply == null || functionalPowerSupply.Owner == null ||
                functionalPowerSupply.Owner.CurrentState != EquipmentState.Placed ||
                !functionalPowerSupply.HasValidACInput)
                return false;

            bool active = functionalPowerSupply.IsActive;
            if (active != (step.PowerSupplyState == ExpectedSwitchState.On))
                return false;
        }

        if (step.FuseState != ExpectedSwitchState.Ignore)
        {
            if (functionalFuse == null || !functionalFuse.IsOperational ||
                functionalFuse.IsBlown)
                return false;

            bool closed = functionalFuse.IsConducting;
            if (closed != (step.FuseState == ExpectedSwitchState.On))
                return false;
        }

        if (step.LampState != ExpectedSwitchState.Ignore)
        {
            if (functionalLamp == null || !functionalLamp.isActiveAndEnabled)
                return false;

            bool on = functionalLamp.IsOn;
            if (on != (step.LampState == ExpectedSwitchState.On))
                return false;
        }

        return true;
    }
}
