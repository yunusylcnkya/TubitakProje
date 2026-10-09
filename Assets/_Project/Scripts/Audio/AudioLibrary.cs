using System;
using UnityEngine;

[Serializable]
public sealed class AudioCueEntry
{
    [SerializeField]
    private GameAudioCue cue;

    [SerializeField]
    private AudioClip clip;

    [SerializeField, Range(0f, 1f)]
    private float volume = 1f;

    [SerializeField]
    private bool useUISource;

    public GameAudioCue Cue =>
        cue;

    public AudioClip Clip =>
        clip;

    public float Volume =>
        volume;

    public bool UseUISource =>
        useUISource;
}


[CreateAssetMenu(
    fileName = "AL_AudioLibrary",
    menuName = "Industrial Automation/Audio/Audio Library"
)]
public sealed class AudioLibrary :
    ScriptableObject
{
    [SerializeField]
    private AudioCueEntry[] entries;

    public bool TryGetEntry(
        GameAudioCue cue,
        out AudioCueEntry result)
    {
        result = null;

        if (entries == null)
            return false;

        for (
            int i = 0;
            i < entries.Length;
            i++)
        {
            AudioCueEntry entry =
                entries[i];

            if (entry == null)
                continue;

            if (entry.Cue == cue)
            {
                result = entry;
                return true;
            }
        }

        return false;
    }
}