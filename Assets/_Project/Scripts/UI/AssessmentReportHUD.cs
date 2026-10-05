using System.Text;
using TMPro;
using UnityEngine;

public sealed class AssessmentReportHUD :
    MonoBehaviour
{
    [Header("Panel")]
    [SerializeField]
    private GameObject panel;

    [Header("Texts")]
    [SerializeField]
    private TMP_Text titleText;

    [SerializeField]
    private TMP_Text scoreText;

    [SerializeField]
    private TMP_Text resultText;

    [SerializeField]
    private TMP_Text detailsText;

    [SerializeField]
    private TMP_Text helpText;

    private void Awake()
    {
        Hide();
    }

    public void Show(
        AssessmentResult result)
    {
        if (result == null)
            return;

        if (panel != null)
        {
            panel.SetActive(true);
        }

        if (titleText != null)
        {
            titleText.text =
                "DEĞERLENDİRME RAPORU";
        }

        if (scoreText != null)
        {
            scoreText.text =
                $"{result.Score} / 100";
        }

        if (resultText != null)
        {
            resultText.text =
                result.Passed
                    ? "BAŞARILI"
                    : "BAŞARISIZ";
        }

        if (detailsText != null)
        {
            detailsText.text =
                BuildDetails(
                    result
                );
        }

        if (helpText != null)
        {
            helpText.text =
                "F10 - Yeni Değerlendirme";
        }
    }

    public void Hide()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    private string BuildDetails(
        AssessmentResult result)
    {
        StringBuilder builder =
            new StringBuilder();

        builder.AppendLine(
            $"Süre: {result.GetFormattedDuration()}"
        );

        builder.AppendLine(
            $"Devre: " +
            $"{(result.CircuitValid ? "Tamamlandı" : "Tamamlanmadı")}"
        );

        builder.AppendLine();

        builder.AppendLine(
            $"Yanlış bağlantı girişimi: " +
            $"{result.WrongConnectionAttempts}"
        );

        builder.AppendLine(
            $"Düzeltilen yanlış bağlantı: " +
            $"{result.CorrectedWrongConnections}"
        );

        builder.AppendLine(
            $"Sökülen kablo: " +
            $"{result.WireRemovals}"
        );

        builder.AppendLine(
            $"Güvenlik ihlali: " +
            $"{result.SafetyViolations}"
        );

        builder.AppendLine();

        builder.AppendLine(
            $"Kısa devre: " +
            $"{result.ShortCircuits}"
        );

        builder.AppendLine(
            $"Faz kaybı: " +
            $"{result.PhaseLosses}"
        );

        builder.AppendLine(
            $"Ters faz sırası: " +
            $"{result.WrongPhaseSequences}"
        );

        builder.AppendLine(
            $"Açık devre: " +
            $"{result.OpenCircuits}"
        );

        builder.AppendLine(
            $"Termik açma: " +
            $"{result.ThermalTrips}"
        );

        builder.AppendLine(
            $"Sigorta atması: " +
            $"{result.FuseBlows}"
        );

        return builder.ToString();
    }
}