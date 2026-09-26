using UnityEngine;
using System.Text;

public sealed class ScenarioUIController :
    MonoBehaviour
{
    [SerializeField]
    private ScenarioRunner scenarioRunner;

    [SerializeField]
    private TaskHUD taskHUD;

    private void OnEnable()
    {
        if (scenarioRunner == null)
            return;

        scenarioRunner.ProgressChanged +=
            Refresh;

        scenarioRunner.ScenarioCompleted +=
            Refresh;
    }

    private void Start()
    {
        Refresh();
    }
    private void RefreshChecklist()
    {
        if (scenarioRunner == null ||
            taskHUD == null ||
            scenarioRunner.Scenario == null)
        {
            return;
        }

        TaskStepDefinition[] steps =
            scenarioRunner.Scenario.Steps;

        StringBuilder builder =
            new StringBuilder();

        for (int i = 0; i < steps.Length; i++)
        {
            bool completed =
                scenarioRunner.IsStepCompleted(i);

            bool current =
                !scenarioRunner.IsCompleted &&
                i == scenarioRunner.CurrentStepIndex;

            string symbol;

            if (completed)
            {
                symbol = "+";
            }
            else if (current)
            {
                symbol = "●";
            }
            else
            {
                symbol = "○";
            }

            builder.Append(symbol);
            builder.Append(" ");
            builder.Append(steps[i].Instruction);

            if (i < steps.Length - 1)
            {
                builder.AppendLine();
            }
        }

        taskHUD.SetChecklist(
            builder.ToString()
        );
    }
    private void OnDisable()
    {
        if (scenarioRunner == null)
            return;

        scenarioRunner.ProgressChanged -=
            Refresh;

        scenarioRunner.ScenarioCompleted -=
            Refresh;
    }

    private void Refresh()
    {
        if (scenarioRunner == null ||
            taskHUD == null ||
            scenarioRunner.Scenario == null)
        {
            return;
        }

        if (scenarioRunner.IsCompleted)
        {
            taskHUD.ShowCompleted(
                scenarioRunner
                    .Scenario
                    .DisplayName,

                scenarioRunner.TotalSteps
            );

            return;
        }

        TaskStepDefinition step =
            scenarioRunner.CurrentStep;

        if (step == null)
            return;

        taskHUD.ShowProgress(
            scenarioRunner
                .Scenario
                .DisplayName,

            scenarioRunner.CompletedStepCount,

            scenarioRunner.TotalSteps,

            step.Instruction
        );
        RefreshChecklist();
    }
}