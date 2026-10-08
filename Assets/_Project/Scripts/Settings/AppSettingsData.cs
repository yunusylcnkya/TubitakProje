using System;
using UnityEngine;

[Serializable]
public sealed class AppSettingsData
{
    [SerializeField]
    private int version = 1;

    [SerializeField, Range(0f, 1f)]
    private float masterVolume = 1f;

    [SerializeField]
    private float mouseSensitivity = 1f;

    [SerializeField]
    private bool fullscreen = true;

    [SerializeField]
    private int resolutionWidth = 1920;

    [SerializeField]
    private int resolutionHeight = 1080;

    [SerializeField]
    private int qualityLevel = 2;

    public int Version =>
        version;

    public float MasterVolume =>
        masterVolume;

    public float MouseSensitivity =>
        mouseSensitivity;

    public bool Fullscreen =>
        fullscreen;

    public int ResolutionWidth =>
        resolutionWidth;

    public int ResolutionHeight =>
        resolutionHeight;

    public int QualityLevel =>
        qualityLevel;

    public void SetValues(
        float newMasterVolume,
        float newMouseSensitivity,
        bool newFullscreen,
        int newResolutionWidth,
        int newResolutionHeight,
        int newQualityLevel)
    {
        masterVolume =
            Mathf.Clamp01(
                newMasterVolume
            );

        mouseSensitivity =
            Mathf.Clamp(
                newMouseSensitivity,
                0.1f,
                5f
            );

        fullscreen =
            newFullscreen;

        resolutionWidth =
            Mathf.Max(
                640,
                newResolutionWidth
            );

        resolutionHeight =
            Mathf.Max(
                480,
                newResolutionHeight
            );

        qualityLevel =
            Mathf.Max(
                0,
                newQualityLevel
            );
    }
}