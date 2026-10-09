using UnityEngine;

public sealed class ThermalRelayAudioBridge :
    MonoBehaviour
{
    [SerializeField]
    private ThermalRelayDevice thermalRelay;

    private bool previousTripped;
    private bool initialized;

    private void OnEnable()
    {
        if (thermalRelay == null)
            return;

        previousTripped =
            thermalRelay.IsTripped;

        initialized = true;

        thermalRelay.StateChanged +=
            HandleStateChanged;
    }

    private void OnDisable()
    {
        if (thermalRelay != null)
        {
            thermalRelay.StateChanged -=
                HandleStateChanged;
        }
    }

    private void HandleStateChanged()
    {
        if (!initialized ||
            thermalRelay == null)
        {
            return;
        }

        bool current =
            thermalRelay.IsTripped;

        if (current ==
            previousTripped)
        {
            return;
        }

        // Trip olduğunda ses çal.
        // Reset için şimdilik ayrı cue kullanmıyoruz.
        if (current)
        {
            AudioManager.Instance?.Play(
                GameAudioCue.ThermalTrip
            );
        }

        previousTripped =
            current;
    }
}