using UnityEngine;

public sealed class ContactorAudioBridge :
    MonoBehaviour
{
    [Header("Device")]
    [SerializeField]
    private ContactorDevice contactor;

    [Header("Debug")]
    [SerializeField]
    private bool debugLogs = false;

    private bool previousEnergized;
    private bool initialized;

    private void Start()
    {
        if (contactor == null)
        {
            contactor =
                GetComponent<ContactorDevice>();
        }

        if (contactor == null)
        {
            Debug.LogError(
                "CONTACTOR AUDIO | ContactorDevice bulunamadı.",
                this
            );

            enabled = false;
            return;
        }

        previousEnergized =
            contactor.IsEnergized;

        initialized = true;

        if (debugLogs)
        {
            Debug.Log(
                $"CONTACTOR AUDIO | Initialized | " +
                $"State: {previousEnergized}",
                this
            );
        }
    }

    private void Update()
    {
        if (!initialized ||
            contactor == null)
        {
            return;
        }

        bool currentEnergized =
            contactor.IsEnergized;

        if (currentEnergized ==
            previousEnergized)
        {
            return;
        }

        if (debugLogs)
        {
            Debug.Log(
                $"CONTACTOR AUDIO | " +
                $"{previousEnergized} -> " +
                $"{currentEnergized}",
                this
            );
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.Play(
                currentEnergized
                    ? GameAudioCue.ContactorEnergized
                    : GameAudioCue.ContactorReleased
            );
        }
        else
        {
            Debug.LogWarning(
                "CONTACTOR AUDIO | AudioManager bulunamadı.",
                this
            );
        }

        previousEnergized =
            currentEnergized;
    }
}