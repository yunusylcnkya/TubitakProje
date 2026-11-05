using System.Text;
using TMPro;
using UnityEngine;

public sealed class EquipmentInfoHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text equipmentNameText;
    [SerializeField] private TMP_Text categoryText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text specificationsText;

    private EquipmentDefinition displayedDefinition;

    private void Awake()
    {
        Hide();
    }

    public void Show(EquipmentDefinition definition)
    {
        if (definition == null)
        {
            Hide();
            return;
        }

        // Do not rewrite four TMP labels for the same equipment definition.
        // Existing labels remain cached when panel is hidden and later shown.
        if (definition != displayedDefinition)
        {
            SetTextIfChanged(equipmentNameText, definition.DisplayName);
            SetTextIfChanged(categoryText, $"Kategori: {definition.Category}");
            SetTextIfChanged(descriptionText, definition.Description);
            SetTextIfChanged(specificationsText, BuildSpecifications(definition));
            displayedDefinition = definition;
        }

        if (panel != null && !panel.activeSelf)
            panel.SetActive(true);
    }

    public void Hide()
    {
        if (panel != null && panel.activeSelf)
            panel.SetActive(false);
    }

    private static void SetTextIfChanged(TMP_Text target, string value)
    {
        if (target == null)
            return;

        value ??= string.Empty;
        if (target.text != value)
            target.text = value;
    }

    private static string BuildSpecifications(EquipmentDefinition definition)
    {
        EquipmentSpecification[] specs = definition.Specifications;
        if (specs == null || specs.Length == 0)
            return string.Empty;

        StringBuilder builder = new StringBuilder();
        foreach (EquipmentSpecification specification in specs)
        {
            if (specification == null)
                continue;

            builder.Append(specification.Label);
            builder.Append(": ");
            builder.AppendLine(specification.Value);
        }
        return builder.ToString();
    }
}
