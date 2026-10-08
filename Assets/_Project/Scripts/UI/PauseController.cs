using UnityEngine;

public sealed class PauseController :
    MonoBehaviour
{
    [Header("UI")]
    [SerializeField]
    private GameObject pausePanel;

    [SerializeField]
    private GameObject settingsPanel;

    [Header("Player Behaviours")]
    [SerializeField]
    private Behaviour[]
        behavioursToDisableWhilePaused;

    [Header("Input")]
    [SerializeField]
    private KeyCode pauseKey =
        KeyCode.Escape;

    private bool isPaused;

    public bool IsPaused =>
        isPaused;

    private void Start()
    {
        ResumeInternal();
    }

    private void Update()
    {
        if (!Input.GetKeyDown(
                pauseKey))
        {
            return;
        }

        if (settingsPanel != null &&
            settingsPanel.activeSelf)
        {
            settingsPanel.SetActive(false);
            return;
        }

        if (isPaused)
        {
            Resume();
        }
        else
        {
            Pause();
        }
    }

    public void Pause()
    {
        if (isPaused)
            return;

        isPaused = true;

        Time.timeScale = 0f;

        SetPlayerBehavioursEnabled(
            false
        );

        SetCursorForMenu();

        if (pausePanel != null)
        {
            pausePanel.SetActive(
                true
            );
        }

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(
                false
            );
        }
    }

    public void Resume()
    {
        if (!isPaused)
            return;

        ResumeInternal();
    }

    public void OpenSettings()
    {
        if (!isPaused)
            return;

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(
                true
            );
        }
    }

    public void RestartTraining()
    {
        PrepareForSceneChange();

        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance
                .ReloadCurrentScene();
        }
    }

    public void ReturnToModuleSelect()
    {
        PrepareForSceneChange();

        TrainingModuleSession.Clear();

        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadScene(
                SceneNames.ModuleSelect
            );
        }
    }

    public void ReturnToMainMenu()
    {
        PrepareForSceneChange();

        TrainingModuleSession.Clear();

        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadScene(
                SceneNames.MainMenu
            );
        }
    }

    private void ResumeInternal()
    {
        isPaused = false;

        Time.timeScale = 1f;

        SetPlayerBehavioursEnabled(
            true
        );

        SetCursorForGameplay();

        if (pausePanel != null)
        {
            pausePanel.SetActive(
                false
            );
        }

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(
                false
            );
        }
    }

    private void PrepareForSceneChange()
    {
        isPaused = false;

        Time.timeScale = 1f;

        SetCursorForMenu();

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(
                false
            );
        }
    }

    private void SetPlayerBehavioursEnabled(
        bool value)
    {
        if (behavioursToDisableWhilePaused ==
            null)
        {
            return;
        }

        for (
            int i = 0;
            i <
            behavioursToDisableWhilePaused.Length;
            i++)
        {
            Behaviour behaviour =
                behavioursToDisableWhilePaused[i];

            if (behaviour == null)
                continue;

            behaviour.enabled =
                value;
        }
    }

    private void SetCursorForGameplay()
    {
        Cursor.lockState =
            CursorLockMode.Locked;

        Cursor.visible =
            false;
    }

    private void SetCursorForMenu()
    {
        Cursor.lockState =
            CursorLockMode.None;

        Cursor.visible =
            true;
    }

    private void OnDisable()
    {
        if (isPaused)
        {
            Time.timeScale = 1f;
        }
    }
}