using UnityEngine;

public sealed class TrainingResultRecorder :
    MonoBehaviour
{
    [Header("Assessment")]
    [SerializeField]
    private AssessmentSystem assessmentSystem;

    [Header("Module")]
    [Tooltip(
        "Scene doğrudan Editor'dan açıldığında " +
        "kullanılacak modül."
    )]
    [SerializeField]
    private TrainingModuleDefinition fallbackModule;

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
        TrainingModuleDefinition module =
            TrainingModuleSession
                .SelectedModule;

        if (module == null)
        {
            module =
                fallbackModule;
        }

        if (module == null)
        {
            Debug.LogWarning(
                "RESULT RECORDER | " +
                "TrainingModuleDefinition bulunamadı.",
                this
            );

            return;
        }

        if (SaveSystem.Instance == null)
        {
            Debug.LogWarning(
                "RESULT RECORDER | " +
                "SaveSystem bulunamadı. " +
                "Boot üzerinden başlatın.",
                this
            );

            return;
        }

        SaveSystem.Instance
            .RecordAssessment(
                module,
                result
            );
    }
}