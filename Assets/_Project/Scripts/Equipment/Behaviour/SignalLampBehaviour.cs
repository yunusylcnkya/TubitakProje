using UnityEngine;

public sealed class SignalLampBehaviour : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private CircuitValidator circuitValidator;

    [SerializeField]
    private Renderer lampRenderer;

    [Header("Visual")]
    [SerializeField]
    private Color offColor =
        new Color(0.2f, 0.2f, 0.2f);

    [SerializeField]
    private Color onColor =
        Color.green;

    private Material runtimeMaterial;

    private void Awake()
    {
        if (lampRenderer != null)
        {
            runtimeMaterial =
                lampRenderer.material;
        }
    }

    private void OnEnable()
    {
        if (circuitValidator != null)
        {
            circuitValidator.ValidationChanged +=
                HandleValidationChanged;
        }
    }

    private void Start()
    {
        TurnOff();
    }

    private void OnDisable()
    {
        if (circuitValidator != null)
        {
            circuitValidator.ValidationChanged -=
                HandleValidationChanged;
        }
    }

    private void HandleValidationChanged(
        CircuitValidationResult result)
    {
        if (result.IsValid)
        {
            TurnOn();
        }
        else
        {
            TurnOff();
        }
    }

    private void TurnOn()
    {
        if (runtimeMaterial == null)
            return;

        runtimeMaterial.color =
            onColor;

        Debug.Log(
            "Signal Lamp ON"
        );
    }

    private void TurnOff()
    {
        if (runtimeMaterial == null)
            return;

        runtimeMaterial.color =
            offColor;

        Debug.Log(
            "Signal Lamp OFF"
        );
    }
}