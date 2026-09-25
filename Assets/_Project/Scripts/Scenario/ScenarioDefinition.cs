using UnityEngine;

[CreateAssetMenu(
    fileName = "ScenarioDefinition",
    menuName =
        "Industrial Automation/Scenario/Scenario Definition"
)]
public sealed class ScenarioDefinition :
    ScriptableObject
{
    [Header("Identity")]
    [SerializeField]
    private string scenarioId;

    [SerializeField]
    private string displayName;

    [TextArea]
    [SerializeField]
    private string description;

    [Header("Steps")]
    [SerializeField]
    private TaskStepDefinition[] steps;

    public string ScenarioId =>
        scenarioId;

    public string DisplayName =>
        displayName;

    public string Description =>
        description;

    public TaskStepDefinition[] Steps =>
        steps;
}