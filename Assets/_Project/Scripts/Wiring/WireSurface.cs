using UnityEngine;

/// <summary>
/// Cable tray hit surface. The hit selects XY; the cable is drawn at the
/// tray's interior bed depth, not at the front cover or a terminal Z plane.
/// Configure once on the CableTray prefab to affect all tray instances.
/// </summary>
public sealed class WireSurface : MonoBehaviour
{
    [Header("Cable bed - world Z")]
    [SerializeField] private bool useChannelBed = true;
    [Tooltip("True if the open/front side of the tray faces negative world Z.")]
    [SerializeField] private bool frontIsNegativeZ = true;
    [Tooltip("Distance from the BACK face toward the open/front side. Increase if the cable is hidden inside the model.")]
    [SerializeField, Min(0f)] private float insetFromBack = 0.015f;
    [Tooltip("Optional explicit world-Z bed reference. If set, collider depth is ignored.")]
    [SerializeField] private Transform channelBedReference;

    [Header("Keep clicks inside the tray")]
    [SerializeField, Min(0f)] private float sideInset = 0.006f;
    [SerializeField] private bool showBedGizmo = true;

    public bool TryGetRoutingPoint(Vector3 hitPoint, out Vector3 result)
    {
        result = hitPoint;
        if (!useChannelBed)
            return true;

        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null || !box.enabled)
            return false;

        Bounds bounds = box.bounds;
        if (bounds.size.z < 0.0001f)
            return false;

        float bedZ = GetBedZ(bounds);
        float x = hitPoint.x;
        float y = hitPoint.y;

        // Do not force the click onto the channel centreline. Only keep it
        // away from the side walls, and leave the lengthwise click untouched.
        bool horizontal = bounds.size.x >= bounds.size.y;
        if (horizontal)
        {
            float margin = Mathf.Min(sideInset, bounds.extents.y * 0.8f);
            y = Mathf.Clamp(y, bounds.min.y + margin, bounds.max.y - margin);
        }
        else
        {
            float margin = Mathf.Min(sideInset, bounds.extents.x * 0.8f);
            x = Mathf.Clamp(x, bounds.min.x + margin, bounds.max.x - margin);
        }

        result = new Vector3(x, y, bedZ);
        return true;
    }

    private float GetBedZ(Bounds bounds)
    {
        float back = frontIsNegativeZ ? bounds.max.z : bounds.min.z;
        float directionToFront = frontIsNegativeZ ? -1f : 1f;
        float depth = channelBedReference != null
            ? channelBedReference.position.z
            : back + directionToFront * insetFromBack;

        // Keep the line inside the collider's depth, even with unusual scales.
        float safety = Mathf.Min(0.001f, bounds.size.z * 0.1f);
        return Mathf.Clamp(depth, bounds.min.z + safety, bounds.max.z - safety);
    }

    private void OnDrawGizmosSelected()
    {
        if (!showBedGizmo || !useChannelBed)
            return;

        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null)
            return;

        Bounds b = box.bounds;
        float z = GetBedZ(b);
        bool horizontal = b.size.x >= b.size.y;
        Vector3 a = horizontal
            ? new Vector3(b.min.x, b.center.y, z)
            : new Vector3(b.center.x, b.min.y, z);
        Vector3 c = horizontal
            ? new Vector3(b.max.x, b.center.y, z)
            : new Vector3(b.center.x, b.max.y, z);
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(a, c);
        Gizmos.DrawSphere(a, 0.004f);
        Gizmos.DrawSphere(c, 0.004f);
    }
}
