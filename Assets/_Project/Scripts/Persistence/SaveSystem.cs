using System;
using System.IO;
using UnityEngine;

[DefaultExecutionOrder(-900)]
public sealed class SaveSystem :
    MonoBehaviour
{
    public static SaveSystem Instance
    {
        get;
        private set;
    }

    [Header("File")]
    [SerializeField]
    private string fileName =
        "automation_training_save.json";

    [SerializeField]
    private bool prettyPrintJson = true;

    private AppSaveData saveData;

    public AppSaveData Data =>
        saveData;

    public StudentRecordData ActiveStudent =>
        saveData != null
            ? saveData.GetActiveStudent()
            : null;

    public bool HasActiveStudent =>
        ActiveStudent != null &&
        ActiveStudent.IsValid;

    private string SavePath =>
        Path.Combine(
            Application.persistentDataPath,
            fileName
        );

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        LoadOrCreate();
    }

    public void SelectOrCreateStudent(
        string fullName,
        string className,
        string studentNumber)
    {
        if (saveData == null)
        {
            saveData =
                new AppSaveData();
        }

        saveData.GetOrCreateStudent(
            fullName,
            className,
            studentNumber
        );

        SaveNow();

        Debug.Log(
            $"SAVE | Aktif öğrenci: " +
            $"{studentNumber} - {fullName}",
            this
        );
    }

    public ModuleProgressData
        GetModuleProgress(
            string moduleId)
    {
        StudentRecordData student =
            ActiveStudent;

        if (student == null)
            return null;

        return student.GetModuleProgress(
            moduleId
        );
    }

    public void RecordAssessment(
        TrainingModuleDefinition module,
        AssessmentResult result)
    {
        if (module == null ||
            result == null)
        {
            return;
        }

        StudentRecordData student =
            ActiveStudent;

        if (student == null)
        {
            Debug.LogWarning(
                "SAVE | Aktif öğrenci yok. " +
                "Assessment sonucu kaydedilmedi.",
                this
            );

            return;
        }

        ModuleProgressData progress =
            student.GetOrCreateModuleProgress(
                module.ModuleId
            );

        progress.RegisterAttempt(
            result
        );

        SaveNow();

        Debug.Log(
            $"SAVE | " +
            $"{student.StudentNumber} | " +
            $"{module.ModuleId} | " +
            $"Score: {result.Score} | " +
            $"Passed: {result.Passed}",
            this
        );
    }

    public void SaveNow()
    {
        if (saveData == null)
        {
            saveData =
                new AppSaveData();
        }

        saveData.EnsureInitialized();

        try
        {
            string json =
                JsonUtility.ToJson(
                    saveData,
                    prettyPrintJson
                );

            File.WriteAllText(
                SavePath,
                json
            );
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"SAVE | Dosya yazılamadı.\n" +
                $"{exception}",
                this
            );
        }
    }

    public void LoadOrCreate()
    {
        if (!File.Exists(
                SavePath))
        {
            saveData =
                new AppSaveData();

            return;
        }

        try
        {
            string json =
                File.ReadAllText(
                    SavePath
                );

            saveData =
                JsonUtility.FromJson<AppSaveData>(
                    json
                );

            if (saveData == null)
            {
                saveData =
                    new AppSaveData();
            }

            saveData.EnsureInitialized();

            Debug.Log(
                "SAVE | Kayıt dosyası yüklendi.",
                this
            );
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"SAVE | Dosya okunamadı. " +
                $"Yeni kayıt oluşturuluyor.\n" +
                $"{exception}",
                this
            );

            saveData =
                new AppSaveData();
        }
    }

    public void ClearActiveStudentProgress()
    {
        StudentRecordData student =
            ActiveStudent;

        if (student == null)
            return;

        student.ClearProgress();

        SaveNow();

        Debug.Log(
            "SAVE | Aktif öğrencinin " +
            "modül ilerlemesi temizlendi.",
            this
        );
    }

    [ContextMenu("Print Save Path")]
    private void PrintSavePath()
    {
        Debug.Log(
            $"SAVE PATH | {SavePath}",
            this
        );
    }

    [ContextMenu("Delete All Save Data")]
    private void DeleteAllSaveData()
    {
        saveData =
            new AppSaveData();

        if (File.Exists(
                SavePath))
        {
            File.Delete(
                SavePath
            );
        }

        Debug.LogWarning(
            "SAVE | Tüm kayıtlar silindi.",
            this
        );
    }
}