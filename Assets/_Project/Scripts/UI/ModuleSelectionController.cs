using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ModuleSelectionController :
    MonoBehaviour
{
    [Header("Catalog")]
    [SerializeField]
    private TrainingModuleCatalog catalog;

    [Header("Module List")]
    [SerializeField]
    private Transform buttonContainer;

    [SerializeField]
    private TrainingModuleButton
        buttonPrefab;

    [Header("Details")]
    [SerializeField]
    private TMP_Text titleText;

    [SerializeField]
    private TMP_Text descriptionText;

    [SerializeField]
    private TMP_Text difficultyText;

    [SerializeField]
    private TMP_Text equipmentListText;

    [Header("Actions")]
    [SerializeField]
    private Button startButton;

    private TrainingModuleDefinition
        selectedModule;

    private void Awake()
    {
        BuildModuleButtons();

        if (startButton != null)
        {
            startButton.onClick.AddListener(
                StartSelectedModule
            );
        }
    }

    private void OnDestroy()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(
                StartSelectedModule
            );
        }
    }
    public void ReturnToMainMenu()
    {
        TrainingModuleSession.Clear();

        if (SceneLoader.Instance == null)
            return;

        SceneLoader.Instance.LoadScene(
            SceneNames.MainMenu
        );
    }
    private void BuildModuleButtons()
    {
        if (catalog == null ||
            buttonPrefab == null ||
            buttonContainer == null)
        {
            Debug.LogError(
                "MODULE MENU | " +
                "Catalog, ButtonPrefab veya " +
                "ButtonContainer eksik.",
                this
            );

            return;
        }

        TrainingModuleDefinition[] modules =
            catalog.Modules;

        if (modules == null ||
            modules.Length == 0)
        {
            Debug.LogWarning(
                "MODULE MENU | " +
                "Catalog boş.",
                this
            );

            return;
        }

        TrainingModuleDefinition first =
            null;

        for (
            int i = 0;
            i < modules.Length;
            i++)
        {
            TrainingModuleDefinition module =
                modules[i];

            if (module == null)
                continue;

            TrainingModuleButton button =
                Instantiate(
                    buttonPrefab,
                    buttonContainer
                );

            button.Bind(
                module,
                SelectModule
            );

            if (first == null)
            {
                first =
                    module;
            }
        }

        if (first != null)
        {
            SelectModule(
                first
            );
        }
    }

    private void SelectModule(
        TrainingModuleDefinition module)
    {
        if (module == null)
            return;

        selectedModule =
            module;

        if (titleText != null)
        {
            titleText.text =
                module.DisplayName;
        }

        if (descriptionText != null)
        {
            descriptionText.text =
                module.Description;
        }

        if (difficultyText != null)
        {
            difficultyText.text =
                $"Zorluk: " +
                $"{GetDifficultyText(module.Difficulty)}";
        }

        if (equipmentListText != null)
        {
            equipmentListText.text =
                BuildEquipmentList(
                    module
                );
        }

        if (startButton != null)
        {
            startButton.interactable =
                !string.IsNullOrWhiteSpace(
                    module.SceneName
                );
        }
    }

    private string BuildEquipmentList(
        TrainingModuleDefinition module)
    {
        EquipmentDefinition[] equipment =
            module.AvailableEquipment;

        if (equipment == null ||
            equipment.Length == 0)
        {
            return
                "Kullanılacak ekipman tanımlanmamış.";
        }

        StringBuilder builder =
            new StringBuilder();

        builder.AppendLine(
            "KULLANILACAK EKİPMANLAR"
        );

        builder.AppendLine();

        for (
            int i = 0;
            i < equipment.Length;
            i++)
        {
            EquipmentDefinition item =
                equipment[i];

            if (item == null)
                continue;

            builder.Append("• ");

            builder.AppendLine(
                item.DisplayName
            );
        }

        return builder.ToString();
    }

    private void StartSelectedModule()
    {
        if (selectedModule == null)
            return;

        if (string.IsNullOrWhiteSpace(
                selectedModule.SceneName))
        {
            Debug.LogError(
                $"MODULE MENU | " +
                $"{selectedModule.DisplayName} " +
                $"için SceneName tanımlanmamış.",
                selectedModule
            );

            return;
        }

        if (SceneLoader.Instance == null)
        {
            Debug.LogError(
                "MODULE MENU | " +
                "SceneLoader bulunamadı.",
                this
            );

            return;
        }

        TrainingModuleSession.SelectModule(
            selectedModule
        );

        Debug.Log(
            $"MODULE START | " +
            $"{selectedModule.ModuleId} | " +
            $"{selectedModule.SceneName}"
        );

        SceneLoader.Instance.LoadScene(
            selectedModule.SceneName
        );
    }

    private string GetDifficultyText(
        TrainingDifficulty difficulty)
    {
        switch (difficulty)
        {
            case TrainingDifficulty.Beginner:
                return "Başlangıç";

            case TrainingDifficulty.Intermediate:
                return "Orta";

            case TrainingDifficulty.Advanced:
                return "İleri";

            default:
                return difficulty.ToString();
        }
    }
}