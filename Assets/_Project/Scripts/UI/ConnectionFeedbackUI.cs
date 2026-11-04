
using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public sealed class ConnectionFeedbackUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField]
    private TMP_Text messageText;

    [Header("Colors")]
    [SerializeField]
    private Color warningColor =
        new Color(1f, 0.75f, 0.2f);

    [SerializeField]
    private Color dangerColor =
        new Color(1f, 0.25f, 0.25f);

    [Header("Settings")]
    [SerializeField, Min(0.5f)]
    private float displayDuration = 4f;

    private CanvasGroup canvasGroup;
    private Coroutine hideCoroutine;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        HideImmediate();
    }

    public void Show(ConnectionValidationResult result)
    {
        Debug.Log(
            $"[CONNECTION UI] Show çağrıldı | " +
            $"Severity: {result.Severity} | " +
            $"Message: {result.Message}",
            this
        );

        if (result.Severity == ConnectionIssueSeverity.None)
            return;

        if (!isActiveAndEnabled)
        {
            Debug.LogError(
                "[CONNECTION UI] Panel veya bileşen aktif değil.",
                this
            );
            return;
        }

        if (messageText == null)
        {
            Debug.LogError(
                "[CONNECTION UI] Message Text referansı eksik.",
                this
            );
            return;
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }

        messageText.gameObject.SetActive(true);
        messageText.enabled = true;

        messageText.text = result.Message;

        messageText.color =
            result.Severity == ConnectionIssueSeverity.Danger
                ? dangerColor
                : warningColor;

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        hideCoroutine =
            StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSecondsRealtime(
            displayDuration
        );

        HideImmediate();
        hideCoroutine = null;
    }

    private void HideImmediate()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    private void OnDisable()
    {
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }

        HideImmediate();
    }

    // =====================================
    // MANUAL DEBUG TESTS
    // =====================================

    [ContextMenu("TEST - Danger Feedback")]
    private void TestDangerFeedback()
    {
        Show(
            new ConnectionValidationResult(
                ConnectionIssueSeverity.Danger,
                "TEST: Faz ile notr arasinda kisa devre riski!",
                false
            )
        );
    }

    [ContextMenu("TEST - Warning Feedback")]
    private void TestWarningFeedback()
    {
        Show(
            new ConnectionValidationResult(
                ConnectionIssueSeverity.Warning,
                "TEST: Terminal baglantisini kontrol edin.",
                true
            )
        );
    }
}
