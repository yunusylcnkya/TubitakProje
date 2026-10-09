using System;
using UnityEngine;

public sealed class InteractionHighlightController :
    MonoBehaviour
{
    [Header("Camera")]
    [SerializeField]
    private Camera playerCamera;

    [Header("Detection")]
    [SerializeField, Min(0.1f)]
    private float detectionDistance = 5f;

    [SerializeField]
    private LayerMask highlightMask = ~0;

    private HighlightTarget currentTarget;

    public HighlightTarget CurrentTarget =>
        currentTarget;

    public bool HasTarget =>
        currentTarget != null;

    public event Action<HighlightTarget>
        TargetChanged;

    private void Update()
    {
        DetectTarget();
    }

    private void DetectTarget()
    {
        if (playerCamera == null)
        {
            SetCurrentTarget(null);
            return;
        }

        Ray ray =
            new Ray(
                playerCamera.transform.position,
                playerCamera.transform.forward
            );

        bool hitSomething =
            Physics.Raycast(
                ray,
                out RaycastHit hit,
                detectionDistance,
                highlightMask,
                QueryTriggerInteraction.Collide
            );

        if (!hitSomething)
        {
            SetCurrentTarget(null);
            return;
        }

        HighlightTarget target =
            hit.collider
                .GetComponentInParent<HighlightTarget>();

        SetCurrentTarget(
            target
        );
    }

    private void SetCurrentTarget(
        HighlightTarget newTarget)
    {
        if (currentTarget ==
            newTarget)
        {
            return;
        }

        if (currentTarget != null)
        {
            currentTarget.SetHighlighted(
                false
            );
        }

        currentTarget =
            newTarget;

        if (currentTarget != null)
        {
            currentTarget.SetHighlighted(
                true
            );
        }

        TargetChanged?.Invoke(
            currentTarget
        );
    }

    private void OnDisable()
    {
        SetCurrentTarget(null);
    }
}