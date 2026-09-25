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

    private void Awake()
    {
        Hide();
    }

    public void Show(
        string target,
        string action)
    {
        if (interactionPanel != null)
        {
            interactionPanel.SetActive(true);
        }

        if (targetText != null)
        {
            targetText.text = target;
        }

        if (actionText != null)
        {
            actionText.text = action;
        }
    }

    public void Hide()
    {
        if (interactionPanel != null)
        {
            interactionPanel.SetActive(false);
        }
    }
}