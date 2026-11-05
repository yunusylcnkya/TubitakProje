using System.Collections.Generic;
using UnityEngine;

public enum ManualWireBendMode
{
    RightAngle90,
    Beveled45
}

/// <summary>
/// A user-chosen endpoint. Corner preference and 45-degree style are captured
/// when clicked, so later Tab/B presses never move previously committed bends.
/// </summary>
public readonly struct ManualWireAnchor
{
    public readonly Vector3 Position;
    public readonly bool HorizontalFirst;
    public readonly bool Use45DegreeCorner;

    public ManualWireAnchor(Vector3 position, bool horizontalFirst,
        bool use45DegreeCorner)
    {
        Position = position;
        HorizontalFirst = horizontalFirst;
        Use45DegreeCorner = use45DegreeCorner;
    }
}

/// <summary>
/// v19.20.7.3: manual, exact-click wiring. No lane allocation, channel-center
/// snapping, pathfinding, or collision-based routing. All route segments are
/// horizontal, vertical, or an optional short 45-degree chamfer at an elbow.
/// </summary>
public sealed class WireManualRouter : MonoBehaviour
{
    [Header("Manual routing controls")]
    [Tooltip("Tab toggles which axis is drawn first for the NEXT segment.")]
    [SerializeField] private bool horizontalFirst = false;

    [Tooltip("B toggles the default for NEXT segments; Shift temporarily inverts it.")]
    [SerializeField]
    private ManualWireBendMode bendMode =
        ManualWireBendMode.RightAngle90;

    [SerializeField, Min(0.001f)] private float chamferLength = 0.020f;
    [SerializeField, Min(0.0001f)] private float minWaypointSpacing = 0.003f;

    [Header("Routing depth (world Z)")]
    [SerializeField, Min(0f)] private float frontOffset = 0.025f;
    [SerializeField] private bool frontIsNegativeZ = true;
    [SerializeField] private bool useFixedRoutingPlaneZ = false;
    [SerializeField] private float fixedRoutingPlaneZ = 0f;

    public bool HorizontalFirst => horizontalFirst;
    public ManualWireBendMode BendMode => bendMode;

    public void ToggleFirstAxis()
    {
        horizontalFirst = !horizontalFirst;
        Debug.Log("Manual wire axis: " +
            (horizontalFirst ? "HORIZONTAL first" : "VERTICAL first"), this);
    }

    public void ToggleBendMode()
    {
        bendMode = bendMode == ManualWireBendMode.RightAngle90
            ? ManualWireBendMode.Beveled45
            : ManualWireBendMode.RightAngle90;
        Debug.Log("Manual wire corner: " + bendMode, this);
    }

    public bool Use45ForCurrentSegment(bool shiftHeld)
    {
        bool use45 = bendMode == ManualWireBendMode.Beveled45;
        return shiftHeld ? !use45 : use45;
    }

    public float GetRoutingZ(Terminal start)
    {
        if (useFixedRoutingPlaneZ)
            return fixedRoutingPlaneZ;

        float startZ = start != null ? start.ConnectionPosition.z : 0f;
        return startZ + (frontIsNegativeZ ? -frontOffset : frontOffset);
    }

    public bool TryCreateAnchor(
        Vector3 clickedWorldPosition,
        Terminal start,
        IReadOnlyList<ManualWireAnchor> committed,
        bool use45,
        out ManualWireAnchor anchor)
    {
        anchor = default;
        if (start == null)
            return false;

        float z = GetRoutingZ(start);
        Vector3 position = new Vector3(
            clickedWorldPosition.x, clickedWorldPosition.y, z);

        Vector3 previous = committed != null && committed.Count > 0
            ? committed[committed.Count - 1].Position
            : new Vector3(start.ConnectionPosition.x,
                start.ConnectionPosition.y, z);

        // Only reject an accidental click on the SAME point. Never reject a
        // point because a channel lane is occupied or the channel has turned.
        if (Vector2.Distance(new Vector2(position.x, position.y),
                new Vector2(previous.x, previous.y)) < minWaypointSpacing)
            return false;

        anchor = new ManualWireAnchor(position, horizontalFirst, use45);
        return true;
    }

    public List<Vector3> BuildPreview(
        Terminal start,
        IReadOnlyList<ManualWireAnchor> committed,
        ManualWireAnchor? hovered)
    {
        return BuildRoute(start, null, committed, hovered,
            horizontalFirst, false);
    }

    public List<Vector3> BuildCompletionRoute(
        Terminal start,
        Terminal end,
        IReadOnlyList<ManualWireAnchor> committed,
        bool finalHorizontalFirst,
        bool finalUse45)
    {
        if (start == null || end == null || start == end)
            return null;
        return BuildRoute(start, end, committed, null,
            finalHorizontalFirst, finalUse45);
    }

    private List<Vector3> BuildRoute(
        Terminal start,
        Terminal end,
        IReadOnlyList<ManualWireAnchor> committed,
        ManualWireAnchor? hovered,
        bool finalHorizontalFirst,
        bool finalUse45)
    {
        if (start == null)
            return null;

        float z = GetRoutingZ(start);
        Vector3 source = start.ConnectionPosition;
        List<Vector3> points = new List<Vector3>();
        AddPoint(points, source);
        // Separate XY routing plane from terminal depth; no diagonal 3D line.
        AddPoint(points, new Vector3(source.x, source.y, z));

        if (committed != null)
        {
            for (int i = 0; i < committed.Count; i++)
            {
                ManualWireAnchor anchor = committed[i];
                AddOrthogonalStep(points, anchor.Position,
                    anchor.HorizontalFirst, anchor.Use45DegreeCorner);
            }
        }

        if (hovered.HasValue)
        {
            ManualWireAnchor anchor = hovered.Value;
            AddOrthogonalStep(points, anchor.Position,
                anchor.HorizontalFirst, anchor.Use45DegreeCorner);
        }

        if (end != null)
        {
            Vector3 target = end.ConnectionPosition;
            AddOrthogonalStep(points,
                new Vector3(target.x, target.y, z),
                finalHorizontalFirst, finalUse45);
            AddPoint(points, target);
        }

        // Wire.SetRoutePreview needs at least two points while starting.
        if (points.Count == 1)
            points.Add(points[0]);
        return points;
    }

    private void AddOrthogonalStep(
        List<Vector3> points, Vector3 destination,
        bool horizontalBeforeVertical, bool use45)
    {
        if (points.Count == 0)
        {
            points.Add(destination);
            return;
        }

        Vector3 from = points[points.Count - 1];
        // All manual anchors use the same routing Z. Defensive depth handling.
        if (Mathf.Abs(from.z - destination.z) > 0.0001f)
        {
            AddPoint(points, new Vector3(from.x, from.y, destination.z));
            from = points[points.Count - 1];
        }

        float dx = destination.x - from.x;
        float dy = destination.y - from.y;
        if (Mathf.Abs(dx) < 0.0001f || Mathf.Abs(dy) < 0.0001f)
        {
            AddPoint(points, destination);
            return;
        }

        // The clicked point is ALWAYS the endpoint, not the snapped channel
        // center. Tab only changes the intermediate elbow, never the endpoint.
        Vector3 corner = horizontalBeforeVertical
            ? new Vector3(destination.x, from.y, destination.z)
            : new Vector3(from.x, destination.y, destination.z);

        if (use45)
        {
            // Bevel ONLY this segment's generated elbow. Previously clicked
            // anchors stay fixed when the user changes 45/90 for the next step.
            float cut = Mathf.Min(chamferLength,
                Mathf.Min(Mathf.Abs(dx), Mathf.Abs(dy)) * 0.35f);
            if (cut >= 0.001f)
            {
                Vector3 before = corner - (corner - from).normalized * cut;
                Vector3 after = corner + (destination - corner).normalized * cut;
                AddPoint(points, before);
                AddPoint(points, after);
                AddPoint(points, destination);
                return;
            }
        }

        AddPoint(points, corner);
        AddPoint(points, destination);
    }

    private static void AddPoint(List<Vector3> points, Vector3 point)
    {
        if (points.Count == 0 ||
            Vector3.Distance(points[points.Count - 1], point) > 0.0001f)
            points.Add(point);
    }
}
