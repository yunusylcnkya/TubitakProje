using System;
using UnityEngine;

public sealed class ScenarioRunner :
    MonoBehaviour
{
    [Header("Scenario")]
    [SerializeField]
    private ScenarioDefinition scenario;

    [Header("Systems")]
    [SerializeField]
    private PlacementManager placementManager;

    [SerializeField]
    private ConnectionManager connectionManager;

    [SerializeField]
    private CircuitValidator circuitValidator;

    [Header("Behaviour")]
    [SerializeField]
    private bool lockAfterCompletion = true;

    private int currentStepIndex;
    private bool isCompleted;

    public event Action ProgressChanged;
    public event Action ScenarioCompleted;

    public ScenarioDefinition Scenario =>
        scenario;

    public int CurrentStepIndex =>
        currentStepIndex;

    public bool IsCompleted =>
        isCompleted;

    public int TotalSteps =>
        scenario != null &&
        scenario.Steps != null
            ? scenario.Steps.Length
            : 0;

    public int CompletedStepCount =>
        isCompleted
            ? TotalSteps
            : currentStepIndex;

    public TaskStepDefinition CurrentStep
    {
        get
        {
            if (scenario == null ||
                scenario.Steps == null)
            {
                return null;
            }

            if (currentStepIndex < 0 ||
                currentStepIndex >=
                scenario.Steps.Length)
            {
                return null;
            }

            return scenario.Steps[
                currentStepIndex
            ];
        }
    }

    private void OnEnable()
    {
        if (placementManager != null)
        {
            placementManager.PlacementsChanged +=
                HandleStateChanged;
        }

        if (connectionManager != null)
        {
            connectionManager.ConnectionsChanged +=
                HandleStateChanged;
        }

        if (circuitValidator != null)
        {
            circuitValidator.ValidationChanged +=
                HandleValidationChanged;
        }
    }

    private void Start()
    {
        StartScenario();
    }

    private void OnDisable()
    {
        if (placementManager != null)
        {
            placementManager.PlacementsChanged -=
                HandleStateChanged;
        }

        if (connectionManager != null)
        {
            connectionManager.ConnectionsChanged -=
                HandleStateChanged;
        }

        if (circuitValidator != null)
        {
            circuitValidator.ValidationChanged -=
                HandleValidationChanged;
        }
    }

    public void StartScenario()
    {
        currentStepIndex = 0;
        isCompleted = false;

        Debug.Log(
            $"Senaryo başladı: " +
            $"{scenario?.DisplayName}"
        );

        RecalculateProgress(true);
    }

    private void HandleStateChanged()
    {
        RecalculateProgress(false);
    }


    public bool IsStepCompleted(int index)
    {
        if (scenario == null ||
            scenario.Steps == null)
        {
            return false;
        }

        if (index < 0 ||
            index >= scenario.Steps.Length)
        {
            return false;
        }

        return IsStepSatisfied(
            scenario.Steps[index]
        );
    }

    private void HandleValidationChanged(
        CircuitValidationResult result)
    {
        RecalculateProgress(false);
    }

    private void RecalculateProgress(
        bool forceNotify)
    {
        if (scenario == null ||
            scenario.Steps == null ||
            scenario.Steps.Length == 0)
        {
            return;
        }

        if (isCompleted &&
            lockAfterCompletion)
        {
            return;
        }

        int previousIndex =
            currentStepIndex;

        int newIndex =
            FindFirstIncompleteStep();

        bool scenarioNowComplete =
            newIndex >= scenario.Steps.Length;

        if (scenarioNowComplete)
        {
            currentStepIndex =
                scenario.Steps.Length;

            if (!isCompleted)
            {
                isCompleted = true;

                Debug.Log(
                    "SENARYO TAMAMLANDI ✓"
                );

                ProgressChanged?.Invoke();
                ScenarioCompleted?.Invoke();
            }

            return;
        }

        isCompleted = false;
        currentStepIndex = newIndex;

        if (forceNotify ||
            previousIndex != currentStepIndex)
        {
            Debug.Log(
                $"Yeni görev: " +
                $"{CurrentStep.Instruction}"
            );

            ProgressChanged?.Invoke();
        }
    }

    private int FindFirstIncompleteStep()
    {
        TaskStepDefinition[] steps =
            scenario.Steps;

        for (int i = 0;
             i < steps.Length;
             i++)
        {
            if (!IsStepSatisfied(
                    steps[i]))
            {
                return i;
            }
        }

        return steps.Length;
    }

    private bool IsStepSatisfied(
        TaskStepDefinition step)
    {
        if (step == null)
            return false;

        switch (step.StepType)
        {
            case TaskStepType.EquipmentPlaced:

                if (placementManager == null)
                    return false;

                return placementManager
                    .IsEquipmentPlaced(
                        step.EquipmentId,
                        step.SlotId
                    );

            case TaskStepType.ConnectionCreated:

                if (connectionManager == null)
                    return false;

                return connectionManager
                    .HasConnection(
                        step.TerminalIdA,
                        step.TerminalIdB
                    );

            case TaskStepType.CircuitValid:

                if (circuitValidator == null)
                    return false;

                return circuitValidator
                    .LastResult
                    .IsValid;

            default:
                return false;
        }
    }
}