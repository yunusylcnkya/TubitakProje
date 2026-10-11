using UnityEngine;

public sealed class RelayAudioBridge :
    MonoBehaviour
{
    [Header("Device")]
    [SerializeField]
    private RelayDevice relay;

    [Header("Debug")]
    [SerializeField]
    private bool debugLogs = false;

    private void Awake()
    {
        if (relay == null)
        {
            relay =
                GetComponent<RelayDevice>();
        }
    }

    private void OnEnable()
    {
        if (relay == null)
        {
            Debug.LogError(
                "RELAY AUDIO | RelayDevice bulunamadı.",
                this
            );

            return;
        }

        relay.EnergizedChanged +=
            HandleEnergizedChanged;
    }

    private void OnDisable()
    {
        if (relay != null)
        {
            relay.EnergizedChanged -=
                HandleEnergizedChanged;
        }
    }

    private void HandleEnergizedChanged(
        bool energized)
    {
        if (debugLogs)
        {
            Debug.Log(
                $"RELAY AUDIO | " +
                $"{(energized ? "ENERGIZED" : "RELEASED")}",
                this
            );
        }

        AudioManager.Instance?.Play(
            energized
                ? GameAudioCue.RelayEnergized
                : GameAudioCue.RelayReleased
        );
    }
}