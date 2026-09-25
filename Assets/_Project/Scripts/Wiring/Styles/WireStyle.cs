using UnityEngine;

[CreateAssetMenu(
    fileName = "WireStyle",
    menuName = "Industrial Automation/Wiring/Wire Style"
)]
public sealed class WireStyle : ScriptableObject
{
    [Header("Identity")]
    [SerializeField]
    private ElectricalLineType lineType;

    [Header("Visual")]
    [SerializeField]
    private Color wireColor = Color.white;

    [SerializeField, Min(0.001f)]
    private float width = 0.015f;

    [SerializeField]
    private Material material;

    public ElectricalLineType LineType => lineType;
    public Color WireColor => wireColor;
    public float Width => width;
    public Material Material => material;
}