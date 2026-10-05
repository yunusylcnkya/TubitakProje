using UnityEngine;

public sealed class AssessmentUIController :
    MonoBehaviour
{
    [Header("System")]
    [SerializeField]
    private AssessmentSystem assessmentSystem;

    [Header("HUD")]
    [SerializeField]
    private AssessmentReportHUD reportHUD;

    [Header("Legacy Input")]
    [SerializeField]
    private KeyCode finishKey =
        KeyCode.F9;

    [SerializeField]
    private KeyCode resetKey =
        KeyCode.F10;

    private void OnEnable()
    {
        if (assessmentSystem != null)
        {
            assessmentSystem.AssessmentCompleted +=
                HandleAssessmentCompleted;
        }
    }

    private void OnDisable()
    {
        if (assessmentSystem != null)
        {
            assessmentSystem.AssessmentCompleted -=
                HandleAssessmentCompleted;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(
                finishKey))
        {
            FinishAssessment();
        }

        if (Input.GetKeyDown(
                resetKey))
        {
            ResetAssessment();
        }
    }

    public void FinishAssessment()
    {
        if (assessmentSystem == null)
            return;

        assessmentSystem
            .FinishAssessment();
    }

    public void ResetAssessment()
    {
        if (assessmentSystem == null)
            return;

        if (reportHUD != null)
        {
            reportHUD.Hide();
        }

        assessmentSystem
            .BeginAssessment();
    }

    private void HandleAssessmentCompleted(
        AssessmentResult result)
    {
        if (reportHUD == null)
            return;

        reportHUD.Show(
            result
        );
    }
}