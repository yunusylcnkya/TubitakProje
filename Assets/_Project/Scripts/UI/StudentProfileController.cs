using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class StudentProfileController :
    MonoBehaviour
{
    [Header("Panel")]
    [SerializeField]
    private GameObject profilePanel;

    [Header("Inputs")]
    [SerializeField]
    private TMP_InputField fullNameInput;

    [SerializeField]
    private TMP_InputField classInput;

    [SerializeField]
    private TMP_InputField studentNumberInput;

    [Header("Texts")]
    [SerializeField]
    private TMP_Text studentSummaryText;

    [SerializeField]
    private TMP_Text statusText;

    [Header("Buttons")]
    [SerializeField]
    private Button trainingButton;

    [SerializeField]
    private Button saveButton;

    [SerializeField]
    private Button editButton;

    [SerializeField]
    private Button cancelButton;

    private void OnEnable()
    {
        if (saveButton != null)
        {
            saveButton.onClick.AddListener(
                SaveStudent
            );
        }

        if (editButton != null)
        {
            editButton.onClick.AddListener(
                ShowEditor
            );
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(
                CloseEditor
            );
        }
    }

    private void Start()
    {
        Refresh();
    }

    private void OnDisable()
    {
        if (saveButton != null)
        {
            saveButton.onClick.RemoveListener(
                SaveStudent
            );
        }

        if (editButton != null)
        {
            editButton.onClick.RemoveListener(
                ShowEditor
            );
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveListener(
                CloseEditor
            );
        }
    }

    public void ShowEditor()
    {
        if (SaveSystem.Instance == null)
            return;

        StudentRecordData student =
            SaveSystem.Instance.ActiveStudent;

        if (student != null)
        {
            if (fullNameInput != null)
            {
                fullNameInput.text =
                    student.FullName;
            }

            if (classInput != null)
            {
                classInput.text =
                    student.ClassName;
            }

            if (studentNumberInput != null)
            {
                studentNumberInput.text =
                    student.StudentNumber;
            }
        }
        else
        {
            ClearInputs();
        }

        if (statusText != null)
        {
            statusText.text =
                string.Empty;
        }

        if (profilePanel != null)
        {
            profilePanel.SetActive(
                true
            );
        }
    }

    public void SaveStudent()
    {
        if (SaveSystem.Instance == null)
        {
            SetStatus(
                "Kayıt sistemi bulunamadı."
            );

            return;
        }

        string fullName =
            fullNameInput != null
                ? fullNameInput.text.Trim()
                : string.Empty;

        string className =
            classInput != null
                ? classInput.text.Trim()
                : string.Empty;

        string studentNumber =
            studentNumberInput != null
                ? studentNumberInput.text.Trim()
                : string.Empty;

        if (string.IsNullOrWhiteSpace(
                fullName))
        {
            SetStatus(
                "Ad Soyad alanını doldurun."
            );

            return;
        }

        if (string.IsNullOrWhiteSpace(
                className))
        {
            SetStatus(
                "Sınıf alanını doldurun."
            );

            return;
        }

        if (string.IsNullOrWhiteSpace(
                studentNumber))
        {
            SetStatus(
                "Öğrenci numarasını doldurun."
            );

            return;
        }

        SaveSystem.Instance
            .SelectOrCreateStudent(
                fullName,
                className,
                studentNumber
            );

        Refresh();

        if (profilePanel != null)
        {
            profilePanel.SetActive(
                false
            );
        }
    }

    public void CloseEditor()
    {
        if (SaveSystem.Instance == null)
            return;

        if (!SaveSystem.Instance
            .HasActiveStudent)
        {
            SetStatus(
                "Eğitime başlamadan önce " +
                "öğrenci bilgisi gereklidir."
            );

            return;
        }

        if (profilePanel != null)
        {
            profilePanel.SetActive(
                false
            );
        }
    }

    private void Refresh()
    {
        if (SaveSystem.Instance == null)
        {
            if (trainingButton != null)
            {
                trainingButton.interactable =
                    false;
            }

            return;
        }

        bool hasStudent =
            SaveSystem.Instance
                .HasActiveStudent;

        if (trainingButton != null)
        {
            trainingButton.interactable =
                hasStudent;
        }

        if (!hasStudent)
        {
            if (studentSummaryText != null)
            {
                studentSummaryText.text =
                    "Öğrenci seçilmedi";
            }

            ShowEditor();
            return;
        }

        StudentRecordData student =
            SaveSystem.Instance
                .ActiveStudent;

        if (studentSummaryText != null)
        {
            studentSummaryText.text =
                $"{student.FullName} | " +
                $"{student.ClassName} | " +
                $"No: {student.StudentNumber}";
        }

        if (statusText != null)
        {
            statusText.text =
                string.Empty;
        }
    }

    private void ClearInputs()
    {
        if (fullNameInput != null)
        {
            fullNameInput.text =
                string.Empty;
        }

        if (classInput != null)
        {
            classInput.text =
                string.Empty;
        }

        if (studentNumberInput != null)
        {
            studentNumberInput.text =
                string.Empty;
        }
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