using UnityEngine;

[CreateAssetMenu(
    fileName = "TC_TrainingCatalog",
    menuName = "Industrial Automation/Training/Training Catalog"
)]
public sealed class TrainingModuleCatalog :
    ScriptableObject
{
    [SerializeField]
    private TrainingModuleDefinition[]
        modules;

    public TrainingModuleDefinition[]
        Modules =>
            modules;
}