using UnityEngine;

public sealed class AssessmentAudioBridge :
    MonoBehaviour
{
    [SerializeField]
    private AssessmentSystem
        assessmentSystem;

    private void OnEnable()
    {
        if (assessmentSystem != null)
        {
            assessmentSystem
                .AssessmentCompleted +=
                HandleAssessmentCompleted;
        }
    }

    private void OnDisable()
    {
        if (assessmentSystem != null)
        {
            assessmentSystem
                .AssessmentCompleted -=
                HandleAssessmentCompleted;
        }
    }

    private void HandleAssessmentCompleted(
        AssessmentResult result)
    {
        if (result == null)
            return;

        AudioManager.Instance?.Play(
            result.Passed
                ? GameAudioCue.AssessmentSuccess
                : GameAudioCue.AssessmentFailed
        );
    }
}