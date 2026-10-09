using UnityEngine;

public enum HighlightTargetType
{
    Generic,
    Equipment,
    PlacementSlot,
    Terminal,
    Control
}

public sealed class HighlightTarget :
    MonoBehaviour
{
    [Header("Type")]
    [SerializeField]
    private HighlightTargetType targetType =
        HighlightTargetType.Generic;

    [Header("Visual")]
    [SerializeField]
    private GameObject highlightVisual;

    public HighlightTargetType TargetType =>
        targetType;

    public bool IsHighlighted
    {
        get;
        private set;
    }

    private void Awake()
    {
        SetHighlighted(false);
    }

    public void SetHighlighted(
        bool value)
    {
        IsHighlighted =
            value;

        if (highlightVisual != null)
        {
            highlightVisual.SetActive(
                value
            );
        }
    }

    private void OnDisable()
    {
        if (highlightVisual != null)
        {
            highlightVisual.SetActive(
                false
            );
        }

        IsHighlighted = false;
    }
}