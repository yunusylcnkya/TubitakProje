using System;
using UnityEngine;

[Serializable]
public sealed class ModuleProgressData
{
    [SerializeField]
    private string moduleId;

    [SerializeField]
    private int attemptCount;

    [SerializeField]
    private int passedAttemptCount;

    [SerializeField]
    private int lastScore;

    [SerializeField]
    private int bestScore;

    [SerializeField]
    private float bestSuccessfulTimeSeconds = -1f;

    [SerializeField]
    private bool completed;

    [SerializeField]
    private string lastPlayedUtc;

    public string ModuleId =>
        moduleId;

    public int AttemptCount =>
        attemptCount;

    public int PassedAttemptCount =>
        passedAttemptCount;

    public int LastScore =>
        lastScore;

    public int BestScore =>
        bestScore;

    public float BestSuccessfulTimeSeconds =>
        bestSuccessfulTimeSeconds;

    public bool Completed =>
        completed;

    public string LastPlayedUtc =>
        lastPlayedUtc;

    public bool HasBestTime =>
        bestSuccessfulTimeSeconds >= 0f;

    public ModuleProgressData()
    {
    }

    public ModuleProgressData(
        string moduleId)
    {
        this.moduleId =
            moduleId?.Trim();
    }

    public void RegisterAttempt(
        AssessmentResult result)
    {
        if (result == null)
            return;

        attemptCount++;

        lastScore =
            result.Score;

        if (result.Score >
            bestScore)
        {
            bestScore =
                result.Score;
        }

        if (result.Passed)
        {
            passedAttemptCount++;
            completed = true;

            if (bestSuccessfulTimeSeconds < 0f ||
                result.DurationSeconds <
                bestSuccessfulTimeSeconds)
            {
                bestSuccessfulTimeSeconds =
                    result.DurationSeconds;
            }
        }

        lastPlayedUtc =
            DateTime.UtcNow.ToString("O");
    }
}