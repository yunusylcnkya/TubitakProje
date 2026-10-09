using UnityEngine;

public sealed class FuseAudioBridge :
    MonoBehaviour
{
    [SerializeField]
    private FuseDevice fuse;

    private bool previousBlown;
    private bool initialized;

    private void OnEnable()
    {
        if (fuse == null)
            return;

        previousBlown =
            fuse.IsBlown;

        initialized = true;

        fuse.StateChanged +=
            HandleStateChanged;
    }

    private void OnDisable()
    {
        if (fuse != null)
        {
            fuse.StateChanged -=
                HandleStateChanged;
        }
    }

    private void HandleStateChanged()
    {
        if (!initialized ||
            fuse == null)
        {
            return;
        }

        bool current =
            fuse.IsBlown;

        if (current ==
            previousBlown)
        {
            return;
        }

        if (current)
        {
            AudioManager.Instance?.Play(
                GameAudioCue.FuseBlown
            );
        }

        previousBlown =
            current;
    }
}