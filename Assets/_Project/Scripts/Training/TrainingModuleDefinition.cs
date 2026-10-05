using UnityEngine;

[CreateAssetMenu(
    fileName = "TM_TrainingModule",
    menuName = "Industrial Automation/Training/Training Module"
)]
public sealed class TrainingModuleDefinition :
    ScriptableObject
{
    [Header("Identity")]
    [SerializeField]
    private string moduleId;

    [SerializeField]
    private int order;

    [SerializeField]
    private string displayName;

    [TextArea(3, 8)]
    [SerializeField]
    private string description;

    [Header("Difficulty")]
    [SerializeField]
    private TrainingDifficulty difficulty;

    [Header("Scene")]
    [SerializeField]
    private string sceneName;

    [Header("Equipment")]
    [SerializeField]
    private EquipmentDefinition[] availableEquipment;

    public string ModuleId =>
        moduleId?.Trim();

    public int Order =>
        order;

    public string DisplayName =>
        displayName;

    public string Description =>
        description;

    public TrainingDifficulty Difficulty =>
        difficulty;

    public string SceneName =>
        sceneName?.Trim();

    public EquipmentDefinition[]
        AvailableEquipment =>
            availableEquipment;

    private void OnValidate()
    {
        if (!string.IsNullOrWhiteSpace(
                moduleId))
        {
            moduleId =
                moduleId.Trim();
        }

        if (!string.IsNullOrWhiteSpace(
                sceneName))
        {
            sceneName =
                sceneName.Trim();
        }

        order =
            Mathf.Max(
                0,
                order
            );
    }
}