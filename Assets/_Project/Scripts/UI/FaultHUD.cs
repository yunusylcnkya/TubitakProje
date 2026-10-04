using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public sealed class FaultHUD : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField]
    private GameObject panel;

    [Header("Texts")]
    [SerializeField]
    private TMP_Text faultTitleText;

    [SerializeField]
    private TMP_Text faultListText;

    [SerializeField]
    private TMP_Text safetyText;

    private bool hasFaults;
    private bool hasSafetyMessage;

    private void Awake()
    {
        if (faultTitleText != null)
        {
            faultTitleText.text =
                "SİSTEM UYARILARI";
        }

        hasFaults = false;
        hasSafetyMessage = false;

        if (faultListText != null)
        {
            faultListText.text =
                string.Empty;
        }

        if (safetyText != null)
        {
            safetyText.text =
                string.Empty;
        }

        RefreshVisibility();
    }

    public void SetFaults(
        IReadOnlyList<ElectricalFault> faults)
    {
        hasFaults =
            faults != null &&
            faults.Count > 0;

        if (faultListText != null)
        {
            faultListText.text =
                BuildFaultText(
                    faults
                );
        }

        RefreshVisibility();
    }

    public void SetSafetyMessage(
        string message)
    {
        hasSafetyMessage =
            !string.IsNullOrWhiteSpace(
                message
            );

        if (safetyText != null)
        {
            safetyText.text =
                message;
        }

        RefreshVisibility();
    }

    public void ClearSafetyMessage()
    {
        hasSafetyMessage = false;

        if (safetyText != null)
        {
            safetyText.text =
                string.Empty;
        }

        RefreshVisibility();
    }

    private string BuildFaultText(
        IReadOnlyList<ElectricalFault> faults)
    {
        if (faults == null ||
            faults.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder builder =
            new StringBuilder();

        for (
            int i = 0;
            i < faults.Count;
            i++)
        {
            ElectricalFault fault =
                faults[i];

            if (fault == null)
                continue;

            builder.Append(
                GetSeverityPrefix(
                    fault.Severity
                )
            );

            builder.Append(" ");

            builder.Append(
                fault.Message
            );

            if (i <
                faults.Count - 1)
            {
                builder.AppendLine();
                builder.AppendLine();
            }
        }

        return builder.ToString();
    }

    private string GetSeverityPrefix(
        FaultSeverity severity)
    {
        switch (severity)
        {
            case FaultSeverity.Danger:
                return "[TEHLİKE]";

            case FaultSeverity.Warning:
                return "[UYARI]";

            case FaultSeverity.Info:
                return "[BİLGİ]";

            default:
                return "[BİLGİ]";
        }
    }

    private void RefreshVisibility()
    {
        if (panel == null)
            return;

        bool shouldBeVisible =
            hasFaults ||
            hasSafetyMessage;

        if (panel.activeSelf !=
            shouldBeVisible)
        {
            panel.SetActive(
                shouldBeVisible
            );
        }
    }
}