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

    [SerializeField]
    private TMP_Text progressText;

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

        RefreshProgress();

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

    public void RefreshProgress()
    {
        if (progressText == null ||
            module == null)
        {
            return;
        }

        if (SaveSystem.Instance == null ||
            !SaveSystem.Instance.HasActiveStudent)
        {
            progressText.text =
                string.Empty;

            return;
        }

        ModuleProgressData progress =
            SaveSystem.Instance
                .GetModuleProgress(
                    module.ModuleId
                );

        if (progress == null ||
            progress.AttemptCount == 0)
        {
            progressText.text =
                "Henüz başlanmadı";

            return;
        }

        if (!progress.Completed)
        {
            progressText.text =
                $"Devam ediyor • " +
                $"En iyi: {progress.BestScore}/100";

            return;
        }

        string timeText =
            progress.HasBestTime
                ? FormatTime(
                    progress
                        .BestSuccessfulTimeSeconds
                )
                : "--:--";

        progressText.text =
            $"Tamamlandı • " +
            $"En iyi: {progress.BestScore}/100 • " +
            $"{timeText}";
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

    private string FormatTime(
        float seconds)
    {
        TimeSpan time =
            TimeSpan.FromSeconds(
                Mathf.Max(
                    0f,
                    seconds
                )
            );

        return
            $"{(int)time.TotalMinutes:00}:" +
            $"{time.Seconds:00}";
    }
}