using System;
using System.IO;
using UnityEngine;

[DefaultExecutionOrder(-850)]
public sealed class SettingsSystem :
    MonoBehaviour
{
    public static SettingsSystem Instance
    {
        get;
        private set;
    }

    [Header("File")]
    [SerializeField]
    private string fileName =
        "automation_training_settings.json";

    [SerializeField]
    private bool prettyPrintJson = true;

    private AppSettingsData settingsData;

    public AppSettingsData Data =>
        settingsData;

    public float MasterVolume =>
        settingsData != null
            ? settingsData.MasterVolume
            : 1f;

    public float MouseSensitivity =>
        settingsData != null
            ? settingsData.MouseSensitivity
            : 1f;

    public event Action SettingsApplied;

    private string SettingsPath =>
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

        ApplyCurrentSettings();
    }

    public void ApplyAndSave(
        float masterVolume,
        float mouseSensitivity,
        bool fullscreen,
        int resolutionWidth,
        int resolutionHeight,
        int qualityLevel)
    {
        if (settingsData == null)
        {
            settingsData =
                CreateDefaultData();
        }

        int qualityCount =
            QualitySettings.names.Length;

        if (qualityCount > 0)
        {
            qualityLevel =
                Mathf.Clamp(
                    qualityLevel,
                    0,
                    qualityCount - 1
                );
        }
        else
        {
            qualityLevel = 0;
        }

        settingsData.SetValues(
            masterVolume,
            mouseSensitivity,
            fullscreen,
            resolutionWidth,
            resolutionHeight,
            qualityLevel
        );

        ApplyCurrentSettings();
        SaveNow();

        Debug.Log(
            $"SETTINGS | Applied | " +
            $"Volume: {settingsData.MasterVolume:0.00} | " +
            $"Mouse: {settingsData.MouseSensitivity:0.00} | " +
            $"Resolution: " +
            $"{settingsData.ResolutionWidth}x" +
            $"{settingsData.ResolutionHeight} | " +
            $"Fullscreen: {settingsData.Fullscreen} | " +
            $"Quality: {settingsData.QualityLevel}",
            this
        );
    }

    public void ResetToDefaults()
    {
        settingsData =
            CreateDefaultData();

        ApplyCurrentSettings();
        SaveNow();

        Debug.Log(
            "SETTINGS | Varsayılan ayarlara dönüldü.",
            this
        );
    }

    public void ApplyCurrentSettings()
    {
        if (settingsData == null)
            return;

        AudioListener.volume =
            Mathf.Clamp01(
                settingsData.MasterVolume
            );

        int qualityCount =
            QualitySettings.names.Length;

        if (qualityCount > 0)
        {
            int quality =
                Mathf.Clamp(
                    settingsData.QualityLevel,
                    0,
                    qualityCount - 1
                );

            QualitySettings.SetQualityLevel(
                quality,
                true
            );
        }

        int width =
            Mathf.Max(
                640,
                settingsData.ResolutionWidth
            );

        int height =
            Mathf.Max(
                480,
                settingsData.ResolutionHeight
            );

        Screen.SetResolution(
            width,
            height,
            settingsData.Fullscreen
        );

        SettingsApplied?.Invoke();
    }

    public void SaveNow()
    {
        if (settingsData == null)
        {
            settingsData =
                CreateDefaultData();
        }

        try
        {
            string json =
                JsonUtility.ToJson(
                    settingsData,
                    prettyPrintJson
                );

            File.WriteAllText(
                SettingsPath,
                json
            );
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"SETTINGS | Dosya yazılamadı.\n" +
                $"{exception}",
                this
            );
        }
    }

    public void LoadOrCreate()
    {
        if (!File.Exists(
                SettingsPath))
        {
            settingsData =
                CreateDefaultData();

            SaveNow();
            return;
        }

        try
        {
            string json =
                File.ReadAllText(
                    SettingsPath
                );

            settingsData =
                JsonUtility
                    .FromJson<AppSettingsData>(
                        json
                    );

            if (settingsData == null)
            {
                settingsData =
                    CreateDefaultData();
            }

            Debug.Log(
                "SETTINGS | Ayarlar yüklendi.",
                this
            );
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"SETTINGS | Dosya okunamadı.\n" +
                $"{exception}",
                this
            );

            settingsData =
                CreateDefaultData();
        }
    }

    private AppSettingsData
        CreateDefaultData()
    {
        AppSettingsData data =
            new AppSettingsData();

        Resolution current =
            Screen.currentResolution;

        int width =
            current.width > 0
                ? current.width
                : Screen.width;

        int height =
            current.height > 0
                ? current.height
                : Screen.height;

        data.SetValues(
            1f,
            1f,
            Screen.fullScreen,
            width,
            height,
            QualitySettings.GetQualityLevel()
        );

        return data;
    }

    [ContextMenu("Print Settings Path")]
    private void PrintSettingsPath()
    {
        Debug.Log(
            $"SETTINGS PATH | {SettingsPath}",
            this
        );
    }

    [ContextMenu("Delete Settings File")]
    private void DeleteSettingsFile()
    {
        if (File.Exists(
                SettingsPath))
        {
            File.Delete(
                SettingsPath
            );
        }

        settingsData =
            CreateDefaultData();

        ApplyCurrentSettings();

        Debug.LogWarning(
            "SETTINGS | Ayar dosyası silindi.",
            this
        );
    }
}