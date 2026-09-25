using UnityEngine;

[CreateAssetMenu(
    fileName = "WireStyleLibrary",
    menuName = "Industrial Automation/Wiring/Wire Style Library"
)]
public sealed class WireStyleLibrary : ScriptableObject
{
    [SerializeField]
    private WireStyle[] styles;

    [SerializeField]
    private WireStyle defaultStyle;

    public WireStyle GetStyle(
        ElectricalLineType lineType)
    {
        foreach (WireStyle style in styles)
        {
            if (style == null)
                continue;

            if (style.LineType == lineType)
                return style;
        }

        return defaultStyle;
    }
}