using System.Text;
using TMPro;
using UnityEngine;

public sealed class EquipmentInfoHUD :
    MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private GameObject panel;

    [SerializeField]
    private TMP_Text equipmentNameText;

    [SerializeField]
    private TMP_Text categoryText;

    [SerializeField]
    private TMP_Text descriptionText;

    [SerializeField]
    private TMP_Text specificationsText;

    private void Awake()
    {
        Hide();
    }

    public void Show(
        EquipmentDefinition definition)
    {
        if (definition == null)
        {
            Hide();
            return;
        }

        if (panel != null)
        {
            panel.SetActive(true);
        }

        if (equipmentNameText != null)
        {
            equipmentNameText.text =
                definition.DisplayName;
        }

        if (categoryText != null)
        {
            categoryText.text =
                $"Kategori: {definition.Category}";
        }

        if (descriptionText != null)
        {
            descriptionText.text =
                definition.Description;
        }

        if (specificationsText != null)
        {
            specificationsText.text =
                BuildSpecifications(
                    definition
                );
        }
    }

    public void Hide()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    private string BuildSpecifications(
        EquipmentDefinition definition)
    {
        EquipmentSpecification[] specs =
            definition.Specifications;

        if (specs == null ||
            specs.Length == 0)
        {
            return string.Empty;
        }

        StringBuilder builder =
            new StringBuilder();

        foreach (
            EquipmentSpecification specification
            in specs)
        {
            if (specification == null)
                continue;

            builder.Append(
                specification.Label
            );

            builder.Append(": ");

            builder.AppendLine(
                specification.Value
            );
        }

        return builder.ToString();
    }
}