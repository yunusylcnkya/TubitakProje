using UnityEngine;

public sealed class MotorAudioBridge :
    MonoBehaviour
{
    [Header("Device")]
    [SerializeField]
    private ThreePhaseMotorDevice motor;

    [Header("Loop Audio")]
    [SerializeField]
    private AudioSource motorLoopSource;

    [Header("Debug")]
    [SerializeField]
    private bool debugLogs = false;

    private MotorRotationDirection previousDirection;
    private bool initialized;

    private void Awake()
    {
        if (motor == null)
        {
            motor =
                GetComponent<ThreePhaseMotorDevice>();
        }

        if (motorLoopSource == null)
        {
            motorLoopSource =
                GetComponent<AudioSource>();
        }

        ConfigureLoopSource();
    }

    private void OnEnable()
    {
        if (motor == null)
        {
            Debug.LogError(
                "MOTOR AUDIO | ThreePhaseMotorDevice bulunamadı.",
                this
            );

            enabled = false;
            return;
        }

        previousDirection =
            motor.RotationDirection;

        initialized = true;

        motor.MotorStateChanged +=
            HandleMotorStateChanged;

        // Obje aktif olduğunda motor zaten çalışıyorsa
        // loop durumunu doğru hale getir.
        if (IsRunning(previousDirection))
        {
            StartMotorLoop();
        }
        else
        {
            StopMotorLoop();
        }

        if (debugLogs)
        {
            Debug.Log(
                $"MOTOR AUDIO | Initialized | " +
                $"State: {previousDirection}",
                this
            );
        }
    }

    private void OnDisable()
    {
        if (motor != null)
        {
            motor.MotorStateChanged -=
                HandleMotorStateChanged;
        }

        StopMotorLoop();

        initialized = false;
    }

    private void HandleMotorStateChanged(
        MotorRotationDirection newDirection)
    {
        if (!initialized)
            return;

        if (newDirection ==
            previousDirection)
        {
            return;
        }

        if (debugLogs)
        {
            Debug.Log(
                $"MOTOR AUDIO | " +
                $"{previousDirection} -> {newDirection}",
                this
            );
        }

        bool wasRunning =
            IsRunning(previousDirection);

        bool isRunning =
            IsRunning(newDirection);

        // DURUYORDU -> ÇALIŞTI
        if (!wasRunning &&
            isRunning)
        {
            AudioManager.Instance?.Play(
                GameAudioCue.MotorStart
            );

            StartMotorLoop();

            if (newDirection ==
                MotorRotationDirection.Reverse)
            {
                AudioManager.Instance?.Play(
                    GameAudioCue.MotorReverse
                );
            }
        }

        // ÇALIŞIYORDU -> DURDU / INVALID
        else if (wasRunning &&
                 !isRunning)
        {
            // Önce sürekli motor sesini anında kes.
            StopMotorLoop();

            AudioManager.Instance?.Play(
                GameAudioCue.MotorStop
            );
        }

        // FORWARD <-> REVERSE
        else if (wasRunning &&
                 isRunning &&
                 previousDirection != newDirection)
        {
            AudioManager.Instance?.Play(
                GameAudioCue.MotorReverse
            );

            // Loop çalışmaya devam eder.
            EnsureMotorLoopRunning();
        }

        previousDirection =
            newDirection;
    }

    private void StartMotorLoop()
    {
        if (motorLoopSource == null)
            return;

        if (AudioManager.Instance == null)
            return;

        if (!AudioManager.Instance.TryGetEntry(
                GameAudioCue.MotorRunningLoop,
                out AudioCueEntry entry))
        {
            Debug.LogWarning(
                "MOTOR AUDIO | MotorRunningLoop AudioLibrary'de bulunamadı.",
                this
            );

            return;
        }

        if (entry == null ||
            entry.Clip == null)
        {
            Debug.LogWarning(
                "MOTOR AUDIO | MotorRunningLoop clip atanmadı.",
                this
            );

            return;
        }

        motorLoopSource.clip =
            entry.Clip;

        motorLoopSource.volume =
            entry.Volume;

        motorLoopSource.loop =
            true;

        if (!motorLoopSource.isPlaying)
        {
            motorLoopSource.Play();
        }

        if (debugLogs)
        {
            Debug.Log(
                "MOTOR AUDIO | Loop START",
                this
            );
        }
    }

    private void StopMotorLoop()
    {
        if (motorLoopSource == null)
            return;

        if (motorLoopSource.isPlaying)
        {
            motorLoopSource.Stop();

            if (debugLogs)
            {
                Debug.Log(
                    "MOTOR AUDIO | Loop STOP",
                    this
                );
            }
        }

        motorLoopSource.clip = null;
    }

    private void EnsureMotorLoopRunning()
    {
        if (motorLoopSource == null ||
            !motorLoopSource.isPlaying)
        {
            StartMotorLoop();
        }
    }

    private void ConfigureLoopSource()
    {
        if (motorLoopSource == null)
            return;

        motorLoopSource.playOnAwake =
            false;

        motorLoopSource.loop =
            true;

        // Motor sesi world-space kaynaklı olsun.
        motorLoopSource.spatialBlend =
            1f;
    }

    private bool IsRunning(
        MotorRotationDirection direction)
    {
        return
            direction ==
                MotorRotationDirection.Forward ||
            direction ==
                MotorRotationDirection.Reverse;
    }
}