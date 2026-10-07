using UnityEngine;
using UnityEngine.UI;

public sealed class MainMenuController :
    MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField]
    private Button trainingButton;

    [SerializeField]
    private Button settingsButton;

    [SerializeField]
    private Button aboutButton;

    [SerializeField]
    private Button quitButton;

    [Header("Panels")]
    [SerializeField]
    private GameObject settingsPanel;

    [SerializeField]
    private GameObject aboutPanel;

    private void Awake()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        if (aboutPanel != null)
        {
            aboutPanel.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (trainingButton != null)
        {
            trainingButton.onClick.AddListener(
                OpenTrainingModules
            );
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(
                OpenSettings
            );
        }

        if (aboutButton != null)
        {
            aboutButton.onClick.AddListener(
                OpenAbout
            );
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(
                QuitGame
            );
        }
    }

    private void OnDisable()
    {
        if (trainingButton != null)
        {
            trainingButton.onClick.RemoveListener(
                OpenTrainingModules
            );
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveListener(
                OpenSettings
            );
        }

        if (aboutButton != null)
        {
            aboutButton.onClick.RemoveListener(
                OpenAbout
            );
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(
                QuitGame
            );
        }
    }

    public void OpenTrainingModules()
    {
        if (SceneLoader.Instance == null)
        {
            Debug.LogError(
                "MAIN MENU | SceneLoader bulunamadı.",
                this
            );

            return;
        }

        SceneLoader.Instance.LoadScene(
            SceneNames.ModuleSelect
        );
    }

    public void OpenSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }

        if (aboutPanel != null)
        {
            aboutPanel.SetActive(false);
        }
    }

    public void OpenAbout()
    {
        if (aboutPanel != null)
        {
            aboutPanel.SetActive(true);
        }

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    public void CloseSecondaryPanels()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        if (aboutPanel != null)
        {
            aboutPanel.SetActive(false);
        }
    }

    public void QuitGame()
    {
        Debug.Log(
            "APPLICATION | Çıkış."
        );

        Application.Quit();
    }
}