using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// v19.20.7.7 - Optional, local spacing assistance for manually selected
/// cable-tray waypoints. It NEVER chooses a tray or rejects a waypoint.
/// It only shifts the cross-tray coordinate if a completed wire already
/// occupies the desired track near the current click.
/// </summary>
public sealed class WireChannelSpacingGuide : MonoBehaviour
{
    [Header("Optional assistance")]
    [SerializeField] private bool enableSpacing = true;
    [SerializeField] private ConnectionManager connectionManager;

    [Header("Spacing in Unity world units")]
    [SerializeField, Min(0.002f)] private float minimumSeparation = 0.022f;
    [SerializeField, Min(0.002f)] private float candidateStep = 0.022f;
    [SerializeField, Range(1, 5)] private int searchSteps = 3;
    [SerializeField, Min(0f)] private float sideWallMargin = 0.008f;
    [SerializeField, Min(0.001f)] private float depthTolerance = 0.018f;
    [SerializeField, Min(0.001f)] private float parallelSegmentMinLength = 0.015f;

    [Header("Continuity along the same tray")]
    [SerializeField] private bool preferPreviousTrack = true;
    [SerializeField, Min(0f)] private float previousTrackTolerance = 0.010f;

    [Header("Diagnostics")]
    [SerializeField] private bool logWhenNoRoom = false;

    private void Awake()
    {
        if (connectionManager == null)
            connectionManager = FindFirstObjectByType<ConnectionManager>();
    }

    public Vector3 AdjustPoint(
        WireSurface surface,
        Vector3 desiredPoint,
        IReadOnlyList<ManualWireAnchor> committed)
    {
        if (!enableSpacing || surface == null || connectionManager == null)
            return desiredPoint;

        BoxCollider box = surface.GetComponent<BoxCollider>();
        if (box == null || !box.enabled)
            return desiredPoint;

        Bounds bounds = box.bounds;
        bool horizontal = bounds.size.x >= bounds.size.y;
        float desiredAxis = horizontal ? desiredPoint.y : desiredPoint.x;
        float along = horizontal ? desiredPoint.x : desiredPoint.y;
        float lower = (horizontal ? bounds.min.y : bounds.min.x) + sideWallMargin;
        float upper = (horizontal ? bounds.max.y : bounds.max.x) - sideWallMargin;

        if (lower > upper)
            return desiredPoint; // Too narrow for spacing; don't block manual wiring.

        // When the next click is on the same parallel tray, preserve the
        // previously chosen cross-tray coordinate unless the player clearly
        // clicks elsewhere. There is NO mandatory lane lock.
        float preferredAxis = desiredAxis;
        if (preferPreviousTrack && committed != null && committed.Count > 0)
        {
            ManualWireAnchor previous = committed[committed.Count - 1];
            if (previous.IsChannelAnchor &&
                Mathf.Abs(previous.Position.z - desiredPoint.z) <= depthTolerance)
            {
                float previousAxis = horizontal ? previous.Position.y : previous.Position.x;
                float previousAlong = horizontal ? previous.Position.x : previous.Position.y;
                bool sameTrackDirection = Mathf.Abs(previousAlong - along) >= 0.005f;
                bool inThisTray = previousAlong >= (horizontal ? bounds.min.x : bounds.min.y) - 0.015f &&
                    previousAlong <= (horizontal ? bounds.max.x : bounds.max.y) + 0.015f;

                if (sameTrackDirection && inThisTray &&
                    Mathf.Abs(previousAxis - desiredAxis) <= previousTrackTolerance)
                    preferredAxis = previousAxis;
            }
        }

        float fromAlong = along;
        if (committed != null && committed.Count > 0)
        {
            ManualWireAnchor previous = committed[committed.Count - 1];
            float prevAlong = horizontal ? previous.Position.x : previous.Position.y;
            float prevCross = horizontal ? previous.Position.y : previous.Position.x;
            if (previous.IsChannelAnchor &&
                Mathf.Abs(previous.Position.z - desiredPoint.z) <= depthTolerance &&
                Mathf.Abs(prevCross - preferredAxis) <= candidateStep * 1.5f &&
                prevAlong >= (horizontal ? bounds.min.x : bounds.min.y) - 0.015f &&
                prevAlong <= (horizontal ? bounds.max.x : bounds.max.y) + 0.015f)
                fromAlong = prevAlong;
        }

        // First try the clicked track unchanged. Only if occupied do we
        // search nearby lateral offsets; offsets are not tied to a grid.
        float chosenAxis = preferredAxis;
        // Preserve the exact clicked coordinate when it is already free.
        if (IsClear(horizontal, chosenAxis, along, fromAlong, desiredPoint.z, bounds))
            return WithAxis(desiredPoint, horizontal, chosenAxis);

        // The requested track is occupied; search only within safe walls.
        // An alternate track must be at least one step away.
        float bestScore = float.PositiveInfinity;
        bool found = false;
        for (int ring = 1; ring <= searchSteps; ring++)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float testAxis = preferredAxis + ring * candidateStep * side;
                if (testAxis < lower || testAxis > upper)
                    continue;
                if (!IsClear(horizontal, testAxis, along, fromAlong, desiredPoint.z, bounds))
                    continue;

                // Stay close to the player's click and prefer the center
                // when two alternatives are otherwise equally good.
                float score = Mathf.Abs(testAxis - desiredAxis) +
                    Mathf.Abs(testAxis - (horizontal ? bounds.center.y : bounds.center.x)) * 0.001f;
                if (score < bestScore)
                {
                    bestScore = score;
                    chosenAxis = testAxis;
                    found = true;
                }
            }
            if (found)
                break;
        }

        if (!found)
        {
            if (logWhenNoRoom)
                Debug.LogWarning("Cable tray: no free nearby track. Manual click retained.", surface);
            return desiredPoint; // Never trap the player or suppress a click.
        }

        return WithAxis(desiredPoint, horizontal, chosenAxis);
    }

    private bool IsClear(
        bool horizontal, float testAxis,
        float endAlong, float startAlong,
        float desiredZ, Bounds channelBounds)
    {
        IReadOnlyList<Connection> connections = connectionManager.Connections;
        if (connections == null)
            return true;

        float checkMin = Mathf.Min(startAlong, endAlong);
        float checkMax = Mathf.Max(startAlong, endAlong);
        // If checking only a single clicked point, consider its immediate
        // neighborhood; for successive channel clicks, consider the span.
        float padding = minimumSeparation * 0.5f;
        checkMin -= padding;
        checkMax += padding;

        float channelMin = horizontal ? channelBounds.min.x : channelBounds.min.y;
        float channelMax = horizontal ? channelBounds.max.x : channelBounds.max.y;
        checkMin = Mathf.Max(checkMin, channelMin);
        checkMax = Mathf.Min(checkMax, channelMax);

        for (int c = 0; c < connections.Count; c++)
        {
            Connection connection = connections[c];
            if (connection == null || connection.Wire == null || !connection.Wire.IsCompleted)
                continue;

            IReadOnlyList<Vector3> route = connection.Wire.RoutePoints;
            if (route == null)
                continue;

            for (int p = 0; p + 1 < route.Count; p++)
            {
                Vector3 a = route[p];
                Vector3 b = route[p + 1];
                if (Mathf.Abs(a.z - desiredZ) > depthTolerance ||
                    Mathf.Abs(b.z - desiredZ) > depthTolerance)
                    continue;

                float crossA = horizontal ? a.y : a.x;
                float crossB = horizontal ? b.y : b.x;
                float alongA = horizontal ? a.x : a.y;
                float alongB = horizontal ? b.x : b.y;

                // Only consider parallel, mostly axis-aligned runs.
                if (Mathf.Abs(crossA - crossB) > 0.003f ||
                    Mathf.Abs(alongA - alongB) < parallelSegmentMinLength)
                    continue;

                float wireMin = Mathf.Min(alongA, alongB);
                float wireMax = Mathf.Max(alongA, alongB);
                if (wireMax < checkMin || wireMin > checkMax)
                    continue;

                if (Mathf.Abs(testAxis - crossA) < minimumSeparation)
                    return false;
            }
        }

        return true;
    }

    private static Vector3 WithAxis(Vector3 original, bool horizontal, float axis)
    {
        return horizontal
            ? new Vector3(original.x, axis, original.z)
            : new Vector3(axis, original.y, original.z);
    }

    private void OnValidate()
    {
        minimumSeparation = Mathf.Max(0.002f, minimumSeparation);
        candidateStep = Mathf.Max(0.002f, candidateStep);
        depthTolerance = Mathf.Max(0.001f, depthTolerance);
    }
}
