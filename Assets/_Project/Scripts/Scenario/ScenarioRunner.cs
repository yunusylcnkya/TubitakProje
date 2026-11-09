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

    [Header("Optional functional verification (Temrin 01+)")]
    [SerializeField] private DCPowerSource functionalPowerSupply;
    [SerializeField] private FuseDevice functionalFuse;
    [SerializeField] private SignalLampBehaviour functionalLamp;
    [SerializeField] private ElectricalSimulationSystem functionalSimulation;

    [Header("Optional START / Contactor verification (Temrin 02+)")]
    [SerializeField] private PushButtonDevice functionalStartButton;
    [SerializeField] private PushButtonDevice functionalStopButton;
    [SerializeField] private ContactorDevice functionalContactor;

    [Header("Optional motor + thermal protection (Temrin 04+)")]
    [SerializeField] private ThreePoleBreakerDevice functionalBreaker;
    [SerializeField] private ThermalRelayDevice functionalThermal;
    [SerializeField] private ThreePhaseMotorDevice functionalMotor;

    [Header("Optional TON Time Relay (Temrin 07+)")]
    [SerializeField] private TimeRelayDevice functionalTimeRelay;

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
        if (functionalStartButton != null)
            functionalStartButton.StateChanged += MarkFunctionalRefresh;
        if (functionalStopButton != null)
            functionalStopButton.StateChanged += MarkFunctionalRefresh;
        if (functionalContactor != null)
            functionalContactor.EnergizedChanged += HandleContactorStateChanged;
        if (functionalBreaker != null)
            functionalBreaker.StateChanged += MarkFunctionalRefresh;
        if (functionalThermal != null)
            functionalThermal.StateChanged += MarkFunctionalRefresh;
        if (functionalMotor != null)
            functionalMotor.MotorStateChanged += HandleMotorStateChanged;
        if (functionalTimeRelay != null)
            functionalTimeRelay.TimeRelayStateChanged += HandleTimeRelayStateChanged;
    }

    private void Start()
    {
        StartScenario();
    }

    // Check functional steps after simulation/lamp events have settled for the frame.
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
        if (functionalStartButton != null)
            functionalStartButton.StateChanged -= MarkFunctionalRefresh;
        if (functionalStopButton != null)
            functionalStopButton.StateChanged -= MarkFunctionalRefresh;
        if (functionalContactor != null)
            functionalContactor.EnergizedChanged -= HandleContactorStateChanged;
        if (functionalBreaker != null)
            functionalBreaker.StateChanged -= MarkFunctionalRefresh;
        if (functionalThermal != null)
            functionalThermal.StateChanged -= MarkFunctionalRefresh;
        if (functionalMotor != null)
            functionalMotor.MotorStateChanged -= HandleMotorStateChanged;
        if (functionalTimeRelay != null)
            functionalTimeRelay.TimeRelayStateChanged -= HandleTimeRelayStateChanged;
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

    private void HandleContactorStateChanged(bool energized)
    {
        MarkFunctionalRefresh();
    }

    private void HandleMotorStateChanged(MotorRotationDirection direction)
    {
        MarkFunctionalRefresh();
    }

    private void HandleTimeRelayStateChanged(TimeRelayState state)
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
                // Sequential/latched: prior functional successes stay recorded.
                if (i >= functionalPassed.Length)
                    return i;
                if (functionalPassed[i])
                    continue;
                if (!IsStepSatisfied(step))
                    return i;
                functionalPassed[i] = true;
                continue;
            }

            // Physical placement, wiring and validation remain live.
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
        // At least one real condition is required for a functional step.
        if (step.PowerSupplyState == ExpectedSwitchState.Ignore &&
            step.FuseState == ExpectedSwitchState.Ignore &&
            step.LampState == ExpectedSwitchState.Ignore &&
            step.StartButtonState == ExpectedSwitchState.Ignore &&
            step.StopButtonState == ExpectedSwitchState.Ignore &&
            step.ContactorState == ExpectedSwitchState.Ignore &&
            step.BreakerState == ExpectedSwitchState.Ignore &&
            step.ThermalTripState == ExpectedSwitchState.Ignore &&
            step.MotorState == ExpectedMotorState.Ignore &&
            step.TimeRelayState == ExpectedTimeRelayState.Ignore)
            return false;

        // No functional test can pass while electrical shorts are present.
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

        if (step.StartButtonState != ExpectedSwitchState.Ignore)
        {
            if (functionalStartButton == null || !functionalStartButton.isActiveAndEnabled ||
                !functionalStartButton.IsOperational)
                return false;

            bool pressed = functionalStartButton.IsPressed;
            if (pressed != (step.StartButtonState == ExpectedSwitchState.On))
                return false;
        }

        if (step.StopButtonState != ExpectedSwitchState.Ignore)
        {
            if (functionalStopButton == null || !functionalStopButton.isActiveAndEnabled ||
                !functionalStopButton.IsOperational)
                return false;

            bool pressed = functionalStopButton.IsPressed;
            if (pressed != (step.StopButtonState == ExpectedSwitchState.On))
                return false;
        }

        if (step.ContactorState != ExpectedSwitchState.Ignore)
        {
            if (functionalContactor == null || !functionalContactor.isActiveAndEnabled ||
                !functionalContactor.IsOperational)
                return false;

            bool energized = functionalContactor.IsEnergized;
            if (energized != (step.ContactorState == ExpectedSwitchState.On))
                return false;
        }

        if (step.BreakerState != ExpectedSwitchState.Ignore)
        {
            if (functionalBreaker == null || !functionalBreaker.isActiveAndEnabled ||
                !functionalBreaker.IsOperational || functionalBreaker.IsTripped)
                return false;

            if (functionalBreaker.IsConducting !=
                (step.BreakerState == ExpectedSwitchState.On))
                return false;
        }

        if (step.ThermalTripState != ExpectedSwitchState.Ignore)
        {
            if (functionalThermal == null || !functionalThermal.isActiveAndEnabled ||
                !functionalThermal.IsOperational)
                return false;

            if (functionalThermal.IsTripped !=
                (step.ThermalTripState == ExpectedSwitchState.On))
                return false;
        }

        if (step.MotorState != ExpectedMotorState.Ignore)
        {
            if (functionalMotor == null || !functionalMotor.isActiveAndEnabled ||
                !functionalMotor.IsOperational)
                return false;

            MotorRotationDirection actual = functionalMotor.RotationDirection;
            switch (step.MotorState)
            {
                case ExpectedMotorState.Stopped:
                    if (actual != MotorRotationDirection.Stopped) return false;
                    break;
                case ExpectedMotorState.Forward:
                    if (actual != MotorRotationDirection.Forward) return false;
                    break;
                case ExpectedMotorState.Reverse:
                    if (actual != MotorRotationDirection.Reverse) return false;
                    break;
                case ExpectedMotorState.Invalid:
                    if (actual != MotorRotationDirection.Invalid) return false;
                    break;
                default:
                    return false;
            }
        }

        if (step.TimeRelayState != ExpectedTimeRelayState.Ignore)
        {
            if (functionalTimeRelay == null ||
                !functionalTimeRelay.isActiveAndEnabled ||
                !functionalTimeRelay.IsOperational ||
                Mathf.Abs(functionalTimeRelay.DelaySeconds - 3f) > 0.01f)
                return false;

            switch (step.TimeRelayState)
            {
                case ExpectedTimeRelayState.Idle:
                    if (functionalTimeRelay.State != TimeRelayState.Idle) return false;
                    break;
                case ExpectedTimeRelayState.Timing:
                    if (functionalTimeRelay.State != TimeRelayState.Timing) return false;
                    break;
                case ExpectedTimeRelayState.Elapsed:
                    if (functionalTimeRelay.State != TimeRelayState.Elapsed) return false;
                    break;
                default:
                    return false;
            }
        }

        return true;
    }
}
