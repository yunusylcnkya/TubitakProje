using UnityEngine;

public sealed class AssessmentUIController : MonoBehaviour
{
    [Header("System")]
    [SerializeField] private AssessmentSystem assessmentSystem;

    [Header("HUD")]
    [SerializeField] private AssessmentReportHUD reportHUD;

    [Header("Optional functional scenario gate (v19.20.8.2)")]
    [SerializeField] private ScenarioRunner requiredScenario;
    [SerializeField] private bool requireScenarioCompletionForFinish = false;

    [Header("Legacy Input")]
    [SerializeField] private KeyCode finishKey = KeyCode.F9;
    [SerializeField] private KeyCode resetKey = KeyCode.F10;

    private void OnEnable()
    {
        if (assessmentSystem != null)
            assessmentSystem.AssessmentCompleted += HandleAssessmentCompleted;
    }

    private void OnDisable()
    {
        if (assessmentSystem != null)
            assessmentSystem.AssessmentCompleted -= HandleAssessmentCompleted;
    }

    private void Update()
    {
        if (Input.GetKeyDown(finishKey))
            FinishAssessment();
        if (Input.GetKeyDown(resetKey))
            ResetAssessment();
    }

    public void FinishAssessment()
    {
        if (assessmentSystem == null)
            return;

        // Other seven training scenes are unaffected (false by default).
        // Guard the normal F9/UI route against awarding a circuit-only pass
        // before the sequential lamp behavior tests have been completed.
        if (requireScenarioCompletionForFinish &&
            (requiredScenario == null || !requiredScenario.IsCompleted))
        {
            Debug.LogWarning(
                "Temrin bitirilmedi. Fonksiyonel test adımlarını tamamlayın.", this);
            return;
        }

        assessmentSystem.FinishAssessment();
    }

    public void ResetAssessment()
    {
        if (assessmentSystem == null)
            return;

        if (reportHUD != null)
            reportHUD.Hide();

        assessmentSystem.BeginAssessment();

        // The same F10 action should restart the functional checklist in
        // this one training scene. Other scenes keep their old behavior.
        if (requireScenarioCompletionForFinish && requiredScenario != null)
            requiredScenario.StartScenario();
    }

    private void HandleAssessmentCompleted(AssessmentResult result)
    {
        if (reportHUD != null)
            reportHUD.Show(result);
    }
}
