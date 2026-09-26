using UnityEngine;

[CreateAssetMenu(
    fileName = "ED_Equipment",
    menuName = "Industrial Automation/Equipment/Equipment Definition"
)]
public sealed class EquipmentDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField]
    private string equipmentId;

    [SerializeField]
    private string displayName;

    [SerializeField]
    private EquipmentCategory category;

    [Header("Education")]
    [TextArea(3, 6)]
    [SerializeField]
    private string description;

    [SerializeField]
    private EquipmentSpecification[] specifications;

    [Header("Visual")]
    [SerializeField]
    private Sprite icon;

    public string EquipmentId =>
        equipmentId?.Trim();

    public string DisplayName =>
        displayName;

    public EquipmentCategory Category =>
        category;

    public string Description =>
        description;

    public EquipmentSpecification[] Specifications =>
        specifications;

    public Sprite Icon =>
        icon;

    private void OnValidate()
    {
        if (!string.IsNullOrWhiteSpace(equipmentId))
        {
            equipmentId =
                equipmentId.Trim();
        }
    }
}