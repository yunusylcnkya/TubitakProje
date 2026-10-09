using UnityEngine;

public sealed class PushButtonAudioBridge :
    MonoBehaviour
{
    [SerializeField]
    private PushButtonDevice pushButton;

    private bool previousPressed;
    private bool initialized;

    private void OnEnable()
    {
        if (pushButton == null)
            return;

        previousPressed =
            pushButton.IsPressed;

        initialized = true;

        pushButton.StateChanged +=
            HandleStateChanged;
    }

    private void OnDisable()
    {
        if (pushButton != null)
        {
            pushButton.StateChanged -=
                HandleStateChanged;
        }
    }

    private void HandleStateChanged()
    {
        if (!initialized ||
            pushButton == null)
        {
            return;
        }

        bool current =
            pushButton.IsPressed;

        if (current ==
            previousPressed)
        {
            return;
        }

        AudioManager.Instance?.Play(
            current
                ? GameAudioCue.ButtonPress
                : GameAudioCue.ButtonRelease
        );

        previousPressed =
            current;
    }
}