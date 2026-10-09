using UnityEngine;

public sealed class SafetyAudioBridge :
    MonoBehaviour
{
    [SerializeField]
    private ElectricalSafetySystem
        safetySystem;

    private void OnEnable()
    {
        if (safetySystem != null)
        {
            safetySystem
                .SafetyViolationRaised +=
                HandleSafetyViolation;
        }
    }

    private void OnDisable()
    {
        if (safetySystem != null)
        {
            safetySystem
                .SafetyViolationRaised -=
                HandleSafetyViolation;
        }
    }

    private void HandleSafetyViolation(
        string message)
    {
        AudioManager.Instance?.Play(
            GameAudioCue.SafetyWarning
        );
    }
}