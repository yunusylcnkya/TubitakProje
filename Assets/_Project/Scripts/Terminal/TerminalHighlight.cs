using UnityEngine;

public sealed class TerminalHighlight : MonoBehaviour
{
    [SerializeField]
    private Renderer targetRenderer;

    [SerializeField]
    private Color normalColor = Color.gray;

    [SerializeField]
    private Color highlightColor = Color.yellow;

    private Material runtimeMaterial;

    private void Awake()
    {
        if (targetRenderer != null)
        {
            runtimeMaterial =
                targetRenderer.material;

            runtimeMaterial.color =
                normalColor;
        }
    }

    public void SetHighlighted(bool highlighted)
    {
        if (runtimeMaterial == null)
            return;

        runtimeMaterial.color =
            highlighted
                ? highlightColor
                : normalColor;
    }
}