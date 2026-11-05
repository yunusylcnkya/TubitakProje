using System.Collections.Generic;
using UnityEngine;

public enum ManualWireBendMode
{
    RightAngle90,
    Beveled45
}

/// <summary>
/// A player-selected waypoint on a WireSurface. The user chooses the channel;
/// the router only snaps the waypoint to a lane and draws orthogonal segments.
/// </summary>
public struct ManualWireAnchor
{
    public readonly Vector3 Position;
    public readonly bool Horizontal;
    public readonly float ChannelCenter;
    public readonly int LaneIndex;

    public ManualWireAnchor(Vector3 position, bool horizontal,
        float channelCenter, int laneIndex)
    {
        Position = position;
        Horizontal = horizontal;
        ChannelCenter = channelCenter;
        LaneIndex = laneIndex;
    }
}

/// <summary>
/// v19.20.7.2 - Guided MANUAL wire routing.
/// The player selects channel waypoints one click at a time.
/// All XY segments are axis-aligned (or 45-degree chamfered at corners).
/// This does not perform automatic pathfinding or electrical validation.
/// </summary>
public sealed class WireManualRouter : MonoBehaviour
{
    [Header("Manual bend appearance")]
    [SerializeField] private ManualWireBendMode bendMode = ManualWireBendMode.RightAngle90;
    [SerializeField, Min(0.005f)] private float terminalStraightLength = 0.045f;
    [SerializeField, Min(0.002f)] private float chamferLength = 0.020f;

    [Header("Routing depth (world Z)")]
    [SerializeField, Min(0.001f)] private float frontOffset = 0.025f;
    [SerializeField] private bool frontIsNegativeZ = true;

    [Header("Channel lanes")]
    [SerializeField, Range(1, 7)] private int lanesPerChannel = 3;
    [SerializeField, Min(0.003f)] private float laneSpacing = 0.022f;
    [SerializeField, Min(0.001f)] private float laneEdgeMargin = 0.003f;

    public bool TryCreateAnchor(
        WireSurface surface,
        Vector3 hitPoint,
        Terminal start,
        IReadOnlyList<Connection> existingConnections,
        IReadOnlyList<ManualWireAnchor> committed,
        out ManualWireAnchor anchor)
    {
        anchor = default;
        if (surface == null || start == null || !surface.isActiveAndEnabled)
            return false;

        BoxCollider collider = surface.GetComponent<BoxCollider>();
        if (collider == null || !collider.enabled)
            return false;

        Bounds bounds = collider.bounds;
        bool horizontal = bounds.size.x >= bounds.size.y;
        float center = horizontal ? bounds.center.y : bounds.center.x;
        float halfWidth = horizontal ? bounds.extents.y : bounds.extents.x;
        float clickedAxis = horizontal ? hitPoint.y : hitPoint.x;
        float along = horizontal
            ? Mathf.Clamp(hitPoint.x, bounds.min.x, bounds.max.x)
            : Mathf.Clamp(hitPoint.y, bounds.min.y, bounds.max.y);

        int laneCount = Mathf.Max(1, lanesPerChannel);
        int centerIndex = laneCount / 2;
        int selectedLane = -1;
        float selectedAxis = center;
        float bestScore = float.PositiveInfinity;

        bool reusePreviousLane = false;
        int previousLane = 0;
        if (committed != null && committed.Count > 0)
        {
            ManualWireAnchor previous = committed[committed.Count - 1];
            reusePreviousLane = previous.Horizontal == horizontal &&
                Mathf.Abs(previous.ChannelCenter - center) <= laneSpacing * 0.6f;
            previousLane = previous.LaneIndex;
        }

        for (int i = 0; i < laneCount; i++)
        {
            float axis = center + (i - centerIndex) * laneSpacing;
            if (Mathf.Abs(axis - center) > halfWidth - laneEdgeMargin)
                continue;

            // When continuing along the same channel, keep the existing lane.
            if (reusePreviousLane && i != previousLane)
                continue;

            if (IsLaneOccupied(
                    horizontal, axis, bounds, existingConnections))
                continue;

            float score = Mathf.Abs(clickedAxis - axis);
            if (score < bestScore)
            {
                bestScore = score;
                selectedLane = i;
                selectedAxis = axis;
            }
        }

        if (selectedLane < 0)
            return false; // Do not silently overlap another wire.

        float z = GetRoutingZ(start);
        Vector3 position = horizontal
            ? new Vector3(along, selectedAxis, z)
            : new Vector3(selectedAxis, along, z);

        anchor = new ManualWireAnchor(position, horizontal, center, selectedLane);
        return true;
    }

    public List<Vector3> BuildPreview(
        Terminal start,
        IReadOnlyList<ManualWireAnchor> committed,
        ManualWireAnchor? hovered)
    {
        List<ManualWireAnchor> anchors = new List<ManualWireAnchor>();
        if (committed != null)
        {
            for (int i = 0; i < committed.Count; i++)
                anchors.Add(committed[i]);
        }

        if (hovered.HasValue)
        {
            ManualWireAnchor next = hovered.Value;
            if (anchors.Count == 0 ||
                Vector3.Distance(anchors[anchors.Count - 1].Position,
                    next.Position) >= 0.002f)
            {
                anchors.Add(next);
            }
        }

        List<Vector3> route = BuildRoute(start, null, anchors);
        if (route != null && route.Count == 1)
            route.Add(route[0]);
        return route;
    }

    public List<Vector3> BuildCompletionRoute(
        Terminal start,
        Terminal end,
        IReadOnlyList<ManualWireAnchor> committed)
    {
        if (start == null || end == null || committed == null || committed.Count == 0)
            return null; // User must select at least one channel.

        return BuildRoute(start, end, committed);
    }

    private List<Vector3> BuildRoute(
        Terminal start,
        Terminal end,
        IReadOnlyList<ManualWireAnchor> anchors)
    {
        if (start == null)
            return null;

        Vector3 a = start.ConnectionPosition;
        float planeZ = GetRoutingZ(start);
        List<Vector3> points = new List<Vector3> { a };

        if (anchors == null || anchors.Count == 0)
        {
            // Only a small, straight initial preview is shown until the first click.
            AddPoint(points, a + Vector3.up * terminalStraightLength);
            return points;
        }

        ManualWireAnchor first = anchors[0];
        Vector3 firstStub = GetTerminalStub(a, first.Position, first.Horizontal);
        AddPoint(points, firstStub);
        AddPoint(points, new Vector3(firstStub.x, firstStub.y, planeZ));

        for (int i = 0; i < anchors.Count; i++)
        {
            ManualWireAnchor next = anchors[i];
            AddOrthogonal(points, next.Position, next.Horizontal);
        }

        if (end != null)
        {
            Vector3 b = end.ConnectionPosition;
            ManualWireAnchor last = anchors[anchors.Count - 1];
            Vector3 endStub = GetTerminalStub(b, last.Position, last.Horizontal);
            Vector3 endStubOnPlane = new Vector3(endStub.x, endStub.y, planeZ);

            // Leaving a horizontal channel: travel horizontally first,
            // then enter the destination vertically. Reverse for vertical channels.
            AddOrthogonal(points, endStubOnPlane, !last.Horizontal);
            AddPoint(points, endStub);
            AddPoint(points, b);
        }

        points = Simplify(points);
        if (bendMode == ManualWireBendMode.Beveled45)
            points = BevelCorners(points);

        return points;
    }

    private Vector3 GetTerminalStub(Vector3 terminal, Vector3 channel, bool horizontal)
    {
        // Never overshoot a channel that is very close to the terminal.
        // If already aligned with the channel, leave along the channel axis.
        if (horizontal)
        {
            float delta = channel.y - terminal.y;
            if (Mathf.Abs(delta) < 0.005f)
            {
                float signX = channel.x >= terminal.x ? 1f : -1f;
                return terminal + Vector3.right * signX * terminalStraightLength;
            }
            float length = Mathf.Min(terminalStraightLength, Mathf.Abs(delta) * 0.65f);
            return terminal + Vector3.up * Mathf.Sign(delta) * length;
        }

        float horizontalDelta = channel.x - terminal.x;
        if (Mathf.Abs(horizontalDelta) < 0.005f)
        {
            float signY = channel.y >= terminal.y ? 1f : -1f;
            return terminal + Vector3.up * signY * terminalStraightLength;
        }
        float horizontalLength = Mathf.Min(terminalStraightLength,
            Mathf.Abs(horizontalDelta) * 0.65f);
        return terminal + Vector3.right * Mathf.Sign(horizontalDelta) * horizontalLength;
    }

    private void AddOrthogonal(List<Vector3> points, Vector3 destination,
        bool verticalFirst)
    {
        if (points.Count == 0)
        {
            points.Add(destination);
            return;
        }

        Vector3 from = points[points.Count - 1];
        if (Mathf.Abs(from.z - destination.z) > 0.0005f)
        {
            // Explicit depth transition; never create diagonal 3D segments.
            AddPoint(points, new Vector3(from.x, from.y, destination.z));
            from = points[points.Count - 1];
        }

        Vector3 corner = verticalFirst
            ? new Vector3(from.x, destination.y, destination.z)
            : new Vector3(destination.x, from.y, destination.z);

        AddPoint(points, corner);
        AddPoint(points, destination);
    }

    private bool IsLaneOccupied(
        bool horizontal,
        float axis,
        Bounds channelBounds,
        IReadOnlyList<Connection> connections)
    {
        if (connections == null)
            return false;

        float min = horizontal ? channelBounds.min.x : channelBounds.min.y;
        float max = horizontal ? channelBounds.max.x : channelBounds.max.y;
        float threshold = laneSpacing * 0.6f;

        foreach (Connection connection in connections)
        {
            if (connection == null || connection.Wire == null)
                continue;

            IReadOnlyList<Vector3> points = connection.Wire.RoutePoints;
            if (points == null)
                continue;

            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector3 p = points[i];
                Vector3 q = points[i + 1];
                float pAxis = horizontal ? p.y : p.x;
                float qAxis = horizontal ? q.y : q.x;
                if (Mathf.Abs(pAxis - axis) > threshold ||
                    Mathf.Abs(qAxis - axis) > threshold)
                    continue;

                float pAlong = horizontal ? p.x : p.y;
                float qAlong = horizontal ? q.x : q.y;
                if (Mathf.Abs(pAlong - qAlong) < 0.005f)
                    continue; // Crossing wires are not parallel occupancy.

                float overlap = Mathf.Min(max, Mathf.Max(pAlong, qAlong)) -
                                Mathf.Max(min, Mathf.Min(pAlong, qAlong));
                if (overlap > 0.005f)
                    return true;
            }
        }
        return false;
    }

    private float GetRoutingZ(Terminal start)
    {
        return start.ConnectionPosition.z +
            (frontIsNegativeZ ? -frontOffset : frontOffset);
    }

    private static void AddPoint(List<Vector3> points, Vector3 p)
    {
        if (points.Count == 0 ||
            Vector3.Distance(points[points.Count - 1], p) > 0.0005f)
            points.Add(p);
    }

    private static List<Vector3> Simplify(List<Vector3> points)
    {
        List<Vector3> result = new List<Vector3>();
        foreach (Vector3 p in points)
        {
            if (result.Count > 0 &&
                Vector3.Distance(result[result.Count - 1], p) <= 0.0005f)
                continue;

            while (result.Count >= 2)
            {
                Vector3 a = result[result.Count - 2];
                Vector3 b = result[result.Count - 1];
                Vector3 ab = b - a;
                Vector3 bc = p - b;
                if (ab.sqrMagnitude < 0.0000001f || bc.sqrMagnitude < 0.0000001f)
                    break;
                if (Vector3.Cross(ab.normalized, bc.normalized).sqrMagnitude >
                        0.000001f || Vector3.Dot(ab, bc) <= 0f)
                    break;
                result.RemoveAt(result.Count - 1);
            }
            result.Add(p);
        }
        return result;
    }

    private List<Vector3> BevelCorners(List<Vector3> points)
    {
        if (points.Count < 3)
            return points;

        List<Vector3> result = new List<Vector3> { points[0] };
        for (int i = 1; i < points.Count - 1; i++)
        {
            Vector3 incoming = points[i] - points[i - 1];
            Vector3 outgoing = points[i + 1] - points[i];
            if (incoming.sqrMagnitude > 0.0000001f &&
                outgoing.sqrMagnitude > 0.0000001f &&
                Mathf.Abs(incoming.z) < 0.0001f &&
                Mathf.Abs(outgoing.z) < 0.0001f &&
                Mathf.Abs(Vector3.Dot(incoming.normalized, outgoing.normalized)) < 0.01f)
            {
                float cut = Mathf.Min(chamferLength,
                    Mathf.Min(incoming.magnitude, outgoing.magnitude) * 0.33f);
                if (cut > 0.002f)
                {
                    AddPoint(result, points[i] - incoming.normalized * cut);
                    AddPoint(result, points[i] + outgoing.normalized * cut);
                    continue;
                }
            }
            AddPoint(result, points[i]);
        }
        AddPoint(result, points[points.Count - 1]);
        return Simplify(result);
    }
}
