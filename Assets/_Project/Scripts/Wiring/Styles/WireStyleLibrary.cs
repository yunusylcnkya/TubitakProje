
using UnityEngine;

[CreateAssetMenu(
    fileName = "WireStyleLibrary",
    menuName = "Industrial Automation/Wiring/Wire Style Library"
)]
public sealed class WireStyleLibrary : ScriptableObject
{
    [Header("Wire Styles")]
    [SerializeField]
    private WireStyle[] styles;

    [Header("Fallback")]
    [SerializeField]
    private WireStyle defaultStyle;

    public WireStyle GetStyle(ElectricalLineType lineType)
    {
        if (styles != null)
        {
            foreach (WireStyle style in styles)
            {
                if (style == null)
                    continue;

                if (style.LineType == lineType)
                    return style;
            }
        }

        Debug.LogWarning(
            $"WireStyleLibrary | " +
            $"'{lineType}' için WireStyle bulunamadı. " +
            $"Varsayılan stil kullanılacak.",
            this
        );

        return defaultStyle;
    }
}
