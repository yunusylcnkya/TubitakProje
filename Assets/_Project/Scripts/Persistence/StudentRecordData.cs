using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class StudentRecordData
{
    [SerializeField]
    private string fullName;

    [SerializeField]
    private string className;

    [SerializeField]
    private string studentNumber;

    [SerializeField]
    private List<ModuleProgressData>
        moduleProgress =
            new List<ModuleProgressData>();

    public string FullName =>
        fullName;

    public string ClassName =>
        className;

    public string StudentNumber =>
        studentNumber;

    public IReadOnlyList<ModuleProgressData>
        ModuleProgress =>
            moduleProgress;

    public bool IsValid =>
        !string.IsNullOrWhiteSpace(fullName) &&
        !string.IsNullOrWhiteSpace(className) &&
        !string.IsNullOrWhiteSpace(studentNumber);

    public void SetIdentity(
        string newFullName,
        string newClassName,
        string newStudentNumber)
    {
        fullName =
            newFullName?.Trim();

        className =
            newClassName?.Trim();

        studentNumber =
            newStudentNumber?.Trim();
    }

    public ModuleProgressData
        GetModuleProgress(
            string moduleId)
    {
        if (string.IsNullOrWhiteSpace(
                moduleId))
        {
            return null;
        }

        EnsureInitialized();

        string id =
            moduleId.Trim();

        for (
            int i = 0;
            i < moduleProgress.Count;
            i++)
        {
            ModuleProgressData progress =
                moduleProgress[i];

            if (progress == null)
                continue;

            if (string.Equals(
                    progress.ModuleId,
                    id,
                    StringComparison.Ordinal))
            {
                return progress;
            }
        }

        return null;
    }

    public ModuleProgressData
        GetOrCreateModuleProgress(
            string moduleId)
    {
        ModuleProgressData existing =
            GetModuleProgress(
                moduleId
            );

        if (existing != null)
            return existing;

        ModuleProgressData created =
            new ModuleProgressData(
                moduleId
            );

        moduleProgress.Add(
            created
        );

        return created;
    }

    public void ClearProgress()
    {
        EnsureInitialized();

        moduleProgress.Clear();
    }

    public void EnsureInitialized()
    {
        if (moduleProgress == null)
        {
            moduleProgress =
                new List<ModuleProgressData>();
        }
    }
}