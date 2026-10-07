using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class SceneLoader :
    MonoBehaviour
{
    public static SceneLoader Instance
    {
        get;
        private set;
    }

    [Header("Loading UI")]
    [SerializeField]
    private GameObject loadingPanel;

    [SerializeField]
    private Slider progressSlider;

    [SerializeField]
    private TMP_Text progressText;

    [SerializeField]
    private TMP_Text sceneText;

    [Header("Behaviour")]
    [SerializeField, Min(0f)]
    private float minimumLoadingTime = 0.4f;

    private bool isLoading;

    public bool IsLoading =>
        isLoading;

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }
    }

    public void LoadScene(
        string sceneName)
    {
        if (isLoading)
            return;

        if (string.IsNullOrWhiteSpace(
                sceneName))
        {
            Debug.LogError(
                "SCENE LOADER | Scene adı boş.",
                this
            );

            return;
        }

        StartCoroutine(
            LoadSceneRoutine(
                sceneName.Trim()
            )
        );
    }

    public void ReloadCurrentScene()
    {
        Scene currentScene =
            SceneManager.GetActiveScene();

        LoadScene(
            currentScene.name
        );
    }

    private IEnumerator LoadSceneRoutine(
        string sceneName)
    {
        isLoading = true;

        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
        }

        if (sceneText != null)
        {
            sceneText.text =
                "Yükleniyor...";
        }

        SetProgress(0f);

        float startTime =
            Time.realtimeSinceStartup;

        AsyncOperation operation =
            SceneManager.LoadSceneAsync(
                sceneName
            );

        if (operation == null)
        {
            Debug.LogError(
                $"SCENE LOADER | " +
                $"{sceneName} yüklenemedi.",
                this
            );

            isLoading = false;
            yield break;
        }

        operation.allowSceneActivation =
            false;

        while (operation.progress < 0.9f)
        {
            float normalized =
                Mathf.Clamp01(
                    operation.progress /
                    0.9f
                );

            SetProgress(
                normalized
            );

            yield return null;
        }

        SetProgress(1f);

        float elapsed =
            Time.realtimeSinceStartup -
            startTime;

        if (elapsed <
            minimumLoadingTime)
        {
            yield return
                new WaitForSecondsRealtime(
                    minimumLoadingTime -
                    elapsed
                );
        }

        operation.allowSceneActivation =
            true;

        while (!operation.isDone)
        {
            yield return null;
        }

        isLoading = false;

        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }
    }

    private void SetProgress(
        float value)
    {
        value =
            Mathf.Clamp01(value);

        if (progressSlider != null)
        {
            progressSlider.value =
                value;
        }

        if (progressText != null)
        {
            progressText.text =
                $"{Mathf.RoundToInt(value * 100f)}%";
        }
    }
}