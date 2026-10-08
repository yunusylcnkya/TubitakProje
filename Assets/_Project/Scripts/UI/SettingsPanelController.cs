using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SettingsPanelController :
    MonoBehaviour
{
    [Header("Panel")]
    [SerializeField]
    private GameObject panel;

    [Header("Audio")]
    [SerializeField]
    private Slider masterVolumeSlider;

    [SerializeField]
    private TMP_Text masterVolumeValueText;

    [Header("Mouse")]
    [SerializeField]
    private Slider mouseSensitivitySlider;

    [SerializeField]
    private TMP_Text mouseSensitivityValueText;

    [Header("Display")]
    [SerializeField]
    private Toggle fullscreenToggle;

    [SerializeField]
    private TMP_Dropdown resolutionDropdown;

    [Header("Graphics")]
    [SerializeField]
    private TMP_Dropdown qualityDropdown;

    [Header("Buttons")]
    [SerializeField]
    private Button applyButton;

    [SerializeField]
    private Button defaultsButton;

    [SerializeField]
    private Button closeButton;

    [Header("Status")]
    [SerializeField]
    private TMP_Text statusText;

    private readonly List<Vector2Int>
        availableResolutions =
            new List<Vector2Int>();

    private void OnEnable()
    {
        RegisterListeners();

        BuildResolutionOptions();
        BuildQualityOptions();

        RefreshFromSettings();
    }

    private void OnDisable()
    {
        UnregisterListeners();
    }

    private void RegisterListeners()
    {
        if (applyButton != null)
        {
            applyButton.onClick.AddListener(
                ApplySettings
            );
        }

        if (defaultsButton != null)
        {
            defaultsButton.onClick.AddListener(
                RestoreDefaults
            );
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(
                Close
            );
        }

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged
                .AddListener(
                    UpdateVolumeLabel
                );
        }

        if (mouseSensitivitySlider != null)
        {
            mouseSensitivitySlider
                .onValueChanged
                .AddListener(
                    UpdateMouseLabel
                );
        }
    }

    private void UnregisterListeners()
    {
        if (applyButton != null)
        {
            applyButton.onClick.RemoveListener(
                ApplySettings
            );
        }

        if (defaultsButton != null)
        {
            defaultsButton.onClick.RemoveListener(
                RestoreDefaults
            );
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(
                Close
            );
        }

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged
                .RemoveListener(
                    UpdateVolumeLabel
                );
        }

        if (mouseSensitivitySlider != null)
        {
            mouseSensitivitySlider
                .onValueChanged
                .RemoveListener(
                    UpdateMouseLabel
                );
        }
    }

    private void BuildResolutionOptions()
    {
        availableResolutions.Clear();

        if (resolutionDropdown == null)
            return;

        resolutionDropdown.ClearOptions();

        List<string> options =
            new List<string>();

        HashSet<string> unique =
            new HashSet<string>();

        Resolution[] resolutions =
            Screen.resolutions;

        for (
            int i = 0;
            i < resolutions.Length;
            i++)
        {
            int width =
                resolutions[i].width;

            int height =
                resolutions[i].height;

            string key =
                $"{width}x{height}";

            if (!unique.Add(key))
                continue;

            availableResolutions.Add(
                new Vector2Int(
                    width,
                    height
                )
            );

            options.Add(
                $"{width} x {height}"
            );
        }

        if (availableResolutions.Count == 0)
        {
            Vector2Int fallback =
                new Vector2Int(
                    Screen.width,
                    Screen.height
                );

            availableResolutions.Add(
                fallback
            );

            options.Add(
                $"{fallback.x} x {fallback.y}"
            );
        }

        resolutionDropdown.AddOptions(
            options
        );
    }

    private void BuildQualityOptions()
    {
        if (qualityDropdown == null)
            return;

        qualityDropdown.ClearOptions();

        List<string> options =
            new List<string>(
                QualitySettings.names
            );

        qualityDropdown.AddOptions(
            options
        );
    }

    private void RefreshFromSettings()
    {
        if (SettingsSystem.Instance == null ||
            SettingsSystem.Instance.Data == null)
        {
            SetStatus(
                "SettingsSystem bulunamadı."
            );

            return;
        }

        AppSettingsData data =
            SettingsSystem.Instance.Data;

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.value =
                data.MasterVolume;
        }

        if (mouseSensitivitySlider != null)
        {
            mouseSensitivitySlider.value =
                data.MouseSensitivity;
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn =
                data.Fullscreen;
        }

        if (qualityDropdown != null)
        {
            int maxIndex =
                Mathf.Max(
                    0,
                    qualityDropdown.options.Count - 1
                );

            qualityDropdown.value =
                Mathf.Clamp(
                    data.QualityLevel,
                    0,
                    maxIndex
                );

            qualityDropdown.RefreshShownValue();
        }

        SelectResolution(
            data.ResolutionWidth,
            data.ResolutionHeight
        );

        UpdateVolumeLabel(
            data.MasterVolume
        );

        UpdateMouseLabel(
            data.MouseSensitivity
        );

        SetStatus(
            string.Empty
        );
    }

    private void SelectResolution(
        int width,
        int height)
    {
        if (resolutionDropdown == null)
            return;

        int selectedIndex = 0;

        for (
            int i = 0;
            i < availableResolutions.Count;
            i++)
        {
            Vector2Int resolution =
                availableResolutions[i];

            if (resolution.x == width &&
                resolution.y == height)
            {
                selectedIndex = i;
                break;
            }
        }

        resolutionDropdown.value =
            selectedIndex;

        resolutionDropdown
            .RefreshShownValue();
    }

    private void ApplySettings()
    {
        if (SettingsSystem.Instance == null)
        {
            SetStatus(
                "SettingsSystem bulunamadı."
            );

            return;
        }

        if (availableResolutions.Count == 0)
            return;

        int resolutionIndex =
            resolutionDropdown != null
                ? resolutionDropdown.value
                : 0;

        resolutionIndex =
            Mathf.Clamp(
                resolutionIndex,
                0,
                availableResolutions.Count - 1
            );

        Vector2Int resolution =
            availableResolutions[
                resolutionIndex
            ];

        float volume =
            masterVolumeSlider != null
                ? masterVolumeSlider.value
                : 1f;

        float mouse =
            mouseSensitivitySlider != null
                ? mouseSensitivitySlider.value
                : 1f;

        bool fullscreen =
            fullscreenToggle != null &&
            fullscreenToggle.isOn;

        int quality =
            qualityDropdown != null
                ? qualityDropdown.value
                : QualitySettings
                    .GetQualityLevel();

        SettingsSystem.Instance
            .ApplyAndSave(
                volume,
                mouse,
                fullscreen,
                resolution.x,
                resolution.y,
                quality
            );

        SetStatus(
            "Ayarlar uygulandı."
        );
    }

    private void RestoreDefaults()
    {
        if (SettingsSystem.Instance == null)
            return;

        SettingsSystem.Instance
            .ResetToDefaults();

        BuildResolutionOptions();
        BuildQualityOptions();

        RefreshFromSettings();

        SetStatus(
            "Varsayılan ayarlar yüklendi."
        );
    }

    public void Close()
    {
        if (panel != null)
        {
            panel.SetActive(
                false
            );
        }
        else
        {
            gameObject.SetActive(
                false
            );
        }
    }

    private void UpdateVolumeLabel(
        float value)
    {
        if (masterVolumeValueText == null)
            return;

        masterVolumeValueText.text =
            $"{Mathf.RoundToInt(value * 100f)}%";
    }

    private void UpdateMouseLabel(
        float value)
    {
        if (mouseSensitivityValueText == null)
            return;

        mouseSensitivityValueText.text =
            value.ToString("0.00");
    }

    private void SetStatus(
        string message)
    {
        if (statusText != null)
        {
            statusText.text =
                message;
        }
    }
}