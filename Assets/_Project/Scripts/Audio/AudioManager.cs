using UnityEngine;

[DefaultExecutionOrder(-800)]
public sealed class AudioManager :
    MonoBehaviour
{
    public static AudioManager Instance
    {
        get;
        private set;
    }

    [Header("Library")]
    [SerializeField]
    private AudioLibrary library;

    [Header("Audio Sources")]
    [SerializeField]
    private AudioSource uiSource;

    [SerializeField]
    private AudioSource sfxSource;

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        ConfigureSource(
            uiSource
        );

        ConfigureSource(
            sfxSource
        );
    }

    public void Play(
        GameAudioCue cue)
    {
        if (library == null)
        {
            Debug.LogWarning(
                "AUDIO | AudioLibrary atanmadı.",
                this
            );

            return;
        }

        if (!library.TryGetEntry(
                cue,
                out AudioCueEntry entry))
        {
            return;
        }

        if (entry == null ||
            entry.Clip == null)
        {
            return;
        }

        AudioSource source =
            entry.UseUISource
                ? uiSource
                : sfxSource;

        if (source == null)
            return;

        source.PlayOneShot(
            entry.Clip,
            entry.Volume
        );
    }

    private void ConfigureSource(
        AudioSource source)
    {
        if (source == null)
            return;

        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
    }

    [ContextMenu("Test UI Click")]
    private void TestUIClick()
    {
        Play(
            GameAudioCue.UIClick
        );
    }

    [ContextMenu("Test Wire Connected")]
    private void TestWireConnected()
    {
        Play(
            GameAudioCue.WireConnected
        );
    }

    [ContextMenu("Test Safety Warning")]
    private void TestSafetyWarning()
    {
        Play(
            GameAudioCue.SafetyWarning
        );
    }
}