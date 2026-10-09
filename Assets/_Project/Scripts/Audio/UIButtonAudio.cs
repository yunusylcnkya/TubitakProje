using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class UIButtonAudio :
    MonoBehaviour,
    IPointerEnterHandler,
    IPointerClickHandler
{
    [Header("Button")]
    [SerializeField]
    private Button button;

    [Header("Cues")]
    [SerializeField]
    private GameAudioCue hoverCue =
        GameAudioCue.UIHover;

    [SerializeField]
    private GameAudioCue clickCue =
        GameAudioCue.UIClick;

    [Header("Behaviour")]
    [SerializeField]
    private bool ignoreWhenNotInteractable =
        true;

    private void Awake()
    {
        if (button == null)
        {
            button =
                GetComponent<Button>();
        }
    }

    public void OnPointerEnter(
        PointerEventData eventData)
    {
        if (!CanPlay())
            return;

        AudioManager.Instance?.Play(
            hoverCue
        );
    }

    public void OnPointerClick(
        PointerEventData eventData)
    {
        if (!CanPlay())
            return;

        AudioManager.Instance?.Play(
            clickCue
        );
    }

    private bool CanPlay()
    {
        if (AudioManager.Instance == null)
            return false;

        if (!ignoreWhenNotInteractable)
            return true;

        if (button == null)
            return true;

        return button.interactable;
    }
}