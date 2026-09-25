using TMPro;
using UnityEngine;

public sealed class TaskHUD : MonoBehaviour
{
    [SerializeField]
    private TMP_Text scenarioTitleText;

    [SerializeField]
    private TMP_Text progressText;

    [SerializeField]
    private TMP_Text currentStepText;

    [SerializeField]
    private TMP_Text checklistText;

    public void ShowProgress(
        string scenarioTitle,
        int completed,
        int total,
        string instruction)
    {
        if (scenarioTitleText != null)
        {
            scenarioTitleText.text =
                scenarioTitle;
        }

        if (progressText != null)
        {
            progressText.text =
                $"Görev {completed + 1} / {total}";
        }

        if (currentStepText != null)
        {
            currentStepText.text =
                instruction;
        }
    }

    public void SetChecklist(string checklist)
    {
        if (checklistText != null)
        {
            checklistText.text =
                checklist;
        }
    }

    public void ShowCompleted(
        string scenarioTitle,
        int total)
    {
        if (scenarioTitleText != null)
        {
            scenarioTitleText.text =
                scenarioTitle;
        }

        if (progressText != null)
        {
            progressText.text =
                $"{total} / {total}";
        }

        if (currentStepText != null)
        {
            currentStepText.text =
                "GÖREV TAMAMLANDI ✓";
        }
    }
}