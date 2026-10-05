using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class TrainingModuleButton :
    MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Button button;

    [SerializeField]
    private TMP_Text titleText;

    [SerializeField]
    private TMP_Text difficultyText;

    private TrainingModuleDefinition
        module;

    public TrainingModuleDefinition Module =>
        module;

    public void Bind(
        TrainingModuleDefinition definition,
        Action<TrainingModuleDefinition>
            selectedCallback)
    {
        module =
            definition;

        if (module == null)
            return;

        if (titleText != null)
        {
            titleText.text =
                $"{module.Order:00} - " +
                $"{module.DisplayName}";
        }

        if (difficultyText != null)
        {
            difficultyText.text =
                GetDifficultyText(
                    module.Difficulty
                );
        }

        if (button != null)
        {
            button.onClick
                .RemoveAllListeners();

            button.onClick.AddListener(
                () =>
                    selectedCallback?.Invoke(
                        module
                    )
            );
        }
    }

    private string GetDifficultyText(
        TrainingDifficulty difficulty)
    {
        switch (difficulty)
        {
            case TrainingDifficulty.Beginner:
                return "Başlangıç";

            case TrainingDifficulty.Intermediate:
                return "Orta";

            case TrainingDifficulty.Advanced:
                return "İleri";

            default:
                return difficulty.ToString();
        }
    }
}