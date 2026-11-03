
using TMPro;
using UnityEngine;

public sealed class InteractionHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private GameObject interactionPanel;

    [SerializeField]
    private TMP_Text targetText;

    [SerializeField]
    private TMP_Text actionText;

    private string lastTarget;
    private string lastAction;

    private void Awake()
    {
        Hide();
    }

    public void Show(string target, string action)
    {
        target ??= string.Empty;
        action ??= string.Empty;

        // Hedef metni değişmediyse tekrar güncelleme.
        if (targetText != null && lastTarget != target)
        {
            targetText.text = target;
            lastTarget = target;
        }

        // İşlem metni değişmediyse tekrar güncelleme.
        if (actionText != null && lastAction != action)
        {
            actionText.text = action;
            lastAction = action;
        }

        // Panel zaten aktifse SetActive çağırma.
        if (interactionPanel != null &&
            !interactionPanel.activeSelf)
        {
            interactionPanel.SetActive(true);
        }
    }

    public void Hide()
    {
        // Panel zaten kapalıysa işlem yapma.
        if (interactionPanel != null &&
            interactionPanel.activeSelf)
        {
            interactionPanel.SetActive(false);
        }
    }
}
