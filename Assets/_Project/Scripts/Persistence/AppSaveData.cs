using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class AppSaveData
{
    [SerializeField]
    private int version = 1;

    [SerializeField]
    private string activeStudentNumber;

    [SerializeField]
    private List<StudentRecordData>
        students =
            new List<StudentRecordData>();

    public int Version =>
        version;

    public string ActiveStudentNumber =>
        activeStudentNumber;

    public IReadOnlyList<StudentRecordData>
        Students =>
            students;

    public void SetActiveStudentNumber(
        string studentNumber)
    {
        activeStudentNumber =
            studentNumber?.Trim();
    }

    public StudentRecordData
        GetStudent(
            string studentNumber)
    {
        if (string.IsNullOrWhiteSpace(
                studentNumber))
        {
            return null;
        }

        EnsureInitialized();

        string number =
            studentNumber.Trim();

        for (
            int i = 0;
            i < students.Count;
            i++)
        {
            StudentRecordData student =
                students[i];

            if (student == null)
                continue;

            if (string.Equals(
                    student.StudentNumber,
                    number,
                    StringComparison.Ordinal))
            {
                return student;
            }
        }

        return null;
    }

    public StudentRecordData
        GetActiveStudent()
    {
        return GetStudent(
            activeStudentNumber
        );
    }

    public StudentRecordData
        GetOrCreateStudent(
            string fullName,
            string className,
            string studentNumber)
    {
        EnsureInitialized();

        StudentRecordData student =
            GetStudent(
                studentNumber
            );

        if (student == null)
        {
            student =
                new StudentRecordData();

            students.Add(
                student
            );
        }

        student.SetIdentity(
            fullName,
            className,
            studentNumber
        );

        activeStudentNumber =
            studentNumber?.Trim();

        return student;
    }

    public void EnsureInitialized()
    {
        if (students == null)
        {
            students =
                new List<StudentRecordData>();
        }

        for (
            int i = 0;
            i < students.Count;
            i++)
        {
            students[i]
                ?.EnsureInitialized();
        }
    }
}