using UnityEngine;

public sealed class TimeRelayAudioBridge :
    MonoBehaviour
{
    [Header("Device")]
    [SerializeField]
    private TimeRelayDevice timeRelay;

    [Header("Debug")]
    [SerializeField]
    private bool debugLogs = false;

    private bool initialized;

    private void Awake()
    {
        if (timeRelay == null)
        {
            timeRelay =
                GetComponent<TimeRelayDevice>();
        }
    }

    private void OnEnable()
    {
        if (timeRelay == null)
        {
            Debug.LogError(
                "TIME RELAY AUDIO | TimeRelayDevice bulunamadı.",
                this
            );

            enabled = false;
            return;
        }

        timeRelay.TimeRelayStateChanged +=
            HandleStateChanged;

        initialized = true;
    }

    private void OnDisable()
    {
        if (timeRelay != null)
        {
            timeRelay.TimeRelayStateChanged -=
                HandleStateChanged;
        }

        initialized = false;
    }

    private void HandleStateChanged(
        TimeRelayState newState)
    {
        if (!initialized)
            return;

        if (debugLogs)
        {
            Debug.Log(
                $"TIME RELAY AUDIO | {newState}",
                this
            );
        }

        if (AudioManager.Instance == null)
            return;

        switch (newState)
        {
            case TimeRelayState.Timing:

                AudioManager.Instance.Play(
                    GameAudioCue
                        .TimeRelayTimingStarted
                );

                break;

            case TimeRelayState.Elapsed:

                AudioManager.Instance.Play(
                    GameAudioCue
                        .TimeRelayElapsed
                );

                break;

            case TimeRelayState.Idle:

                AudioManager.Instance.Play(
                    GameAudioCue
                        .TimeRelayReset
                );

                break;
        }
    }
}