using System.Collections.Generic;
using UnityEngine;

public enum ManualWireBendMode
{
    RightAngle90,
    Beveled45
}

/// <summary>
/// The exact player-selected XY waypoint, at the selected surface's Z depth.
/// The turn choice is captured when clicked; future Tab/B changes cannot
/// modify an already committed segment.
/// </summary>
public readonly struct ManualWireAnchor
{
    public readonly Vector3 Position;
    public readonly bool HorizontalFirst;
    public readonly bool Use45DegreeCorner;
    public readonly bool IsChannelAnchor;

    public ManualWireAnchor(Vector3 position, bool horizontalFirst,
        bool use45DegreeCorner, bool isChannelAnchor = false)
    {
        Position = position;
        HorizontalFirst = horizontalFirst;
        Use45DegreeCorner = use45DegreeCorner;
        IsChannelAnchor = isChannelAnchor;
    }
}

/// <summary>
/// v19.20.7.6 - manual waypoints with optional symmetric 90-degree
/// terminal-to-channel entry and channel-to-terminal exit.
/// The player chooses the XY route. WireSurface chooses the channel-bed Z.
/// No lane reservation, auto pathfinding, or electrical logic changes.
/// </summary>
public sealed class WireManualRouter : MonoBehaviour
{
    [Header("Optional channel spacing - v19.20.7.7")]
    [SerializeField]
    private WireChannelSpacingGuide channelSpacingGuide;
    [Header("Manual routing controls")]
    [SerializeField] private bool horizontalFirst = false;
    [SerializeField]
    private ManualWireBendMode bendMode =
        ManualWireBendMode.RightAngle90;
    [SerializeField, Min(0.001f)] private float chamferLength = 0.020f;
    [SerializeField, Min(0.0001f)] private float minWaypointSpacing = 0.003f;

    [Header("Terminal-to-channel slope")]
    [Tooltip("Angle of the last XY segment when changing Z to enter or exit a tray.")]
    [SerializeField, Range(10f, 80f)] private float entrySlopeAngle = 45f;
    [Tooltip("Keep some of the last XY segment for a visible slope.")]
    [SerializeField, Range(0.2f, 0.95f)] private float maxSlopeFraction = 0.85f;

    [Header("Orthogonal terminal approaches")]
    [Tooltip("From the source terminal, leave in a short straight line, " +
             "turn 90 degrees towards the first chosen channel point, " +
             "and enter the channel at its selected depth. " +
             "Mirrors the final terminal approach.")]
    [SerializeField] private bool useOrthogonalTerminalEntry = true;

    [Tooltip("From the last chosen channel point, travel to the destination " +
             "with 90-degree turns and a short straight terminal approach.")]
    [SerializeField] private bool useOrthogonalTerminalExit = true;
    [Tooltip("Length of the final straight segment before entering the terminal " +
             "(world units; 0.025 = 2.5 cm if 1 unit = 1 meter).")]
    [SerializeField, Min(0.005f)] private float terminalStraightLength = 0.025f;

    [Header("Free-space routing depth (world Z)")]
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
        WireSurface surface,
        Terminal start,
        IReadOnlyList<ManualWireAnchor> committed,
        bool use45,
        out ManualWireAnchor anchor)
    {
        anchor = default;
        if (start == null)
            return false;

        Vector3 position;
        if (surface != null)
        {
            if (!surface.TryGetRoutingPoint(clickedWorldPosition, out position))
                return false;
        }
        else
        {
            position = new Vector3(clickedWorldPosition.x,
                clickedWorldPosition.y, GetRoutingZ(start));
        }
        if (surface != null &&
            channelSpacingGuide != null &&
            channelSpacingGuide.isActiveAndEnabled)
        {
            position = channelSpacingGuide.AdjustPoint(
                surface, position, committed);
        }
        Vector3 previous = committed != null && committed.Count > 0
            ? committed[committed.Count - 1].Position
            : start.ConnectionPosition;

        // Allow a second click at the same XY when it changes Z meaningfully;
        // otherwise ignore duplicate waypoints.
        if (Vector3.Distance(position, previous) < minWaypointSpacing)
            return false;

        anchor = new ManualWireAnchor(position, horizontalFirst, use45,
            surface != null);
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

        // Start at the actual terminal depth. Do not jump to a global plane.
        List<Vector3> points = new List<Vector3>();
        AddPoint(points, start.ConnectionPosition);

        // The first channel waypoint uses a symmetric, 90-degree exit from
        // the SOURCE terminal. All subsequent waypoints remain fully manual.
        // This rule is also applied to the hover preview, so the clicked
        // route matches what was shown before clicking.
        bool isFirstWaypoint = true;
        if (committed != null)
        {
            for (int i = 0; i < committed.Count; i++)
            {
                ManualWireAnchor a = committed[i];
                AppendManualWaypoint(points, a, isFirstWaypoint);
                isFirstWaypoint = false;
            }
        }

        if (hovered.HasValue)
        {
            AppendManualWaypoint(points, hovered.Value, isFirstWaypoint);
        }

        if (end != null)
        {
            // Keep the existing sloped behavior for routes without a channel
            // waypoint. For a channel exit, use the player's final channel
            // waypoint and create an orthogonal terminal approach.
            bool lastAnchorIsChannel = committed != null &&
                committed.Count > 0 &&
                committed[committed.Count - 1].IsChannelAnchor;

            if (useOrthogonalTerminalExit && lastAnchorIsChannel)
                AddOrthogonalTerminalExit(points, end.ConnectionPosition);
            else
                AddStep(points, end.ConnectionPosition,
                    finalHorizontalFirst, finalUse45);
        }

        if (points.Count == 1)
            points.Add(points[0]);
        return points;
    }

    // First channel click: leave the SOURCE terminal using the exact
    // reverse geometry of the existing orthogonal destination approach.
    // The user's selected channel waypoint is not moved or lane-snapped.
    private void AppendManualWaypoint(
        List<Vector3> points,
        ManualWireAnchor anchor,
        bool isFirstWaypoint)
    {
        if (isFirstWaypoint &&
            useOrthogonalTerminalEntry &&
            anchor.IsChannelAnchor)
        {
            AddOrthogonalTerminalEntry(points, anchor.Position);
            return;
        }

        AddStep(points, anchor.Position,
            anchor.HorizontalFirst, anchor.Use45DegreeCorner);
    }

    private void AddOrthogonalTerminalEntry(
        List<Vector3> points, Vector3 channelPosition)
    {
        if (points == null || points.Count == 0)
            return;

        Vector3 sourceTerminal = points[points.Count - 1];

        // Build from channel back to the source with the existing,
        // tested channel-to-terminal geometry. Reversing these points
        // gives a matching source-to-channel entry without modifying
        // any already-committed route segments.
        List<Vector3> reverse = new List<Vector3> { channelPosition };
        AddOrthogonalTerminalExit(reverse, sourceTerminal);

        // Reverse ends at the original source terminal, already in points.
        // Append remaining positions in reverse order to reach the channel.
        for (int i = reverse.Count - 2; i >= 0; i--)
            AddPoint(points, reverse[i]);
    }

    /// <summary>
    /// From the last user-selected channel point, travel horizontally to the
    /// terminal's X, turn 90 degrees up/down, change Z depth at the end of
    /// the rise, then enter the terminal on a short straight XY segment.
    /// No automatic channel selection or lane snapping is introduced.
    /// </summary>
    private void AddOrthogonalTerminalExit(
        List<Vector3> points, Vector3 terminalPosition)
    {
        if (points.Count == 0)
        {
            AddPoint(points, terminalPosition);
            return;
        }

        Vector3 from = points[points.Count - 1];
        float verticalDistance = terminalPosition.y - from.y;
        float horizontalDistance = terminalPosition.x - from.x;

        // Normal electrical-panel case: the channel is above or below the
        // terminal. The final visible approach is vertical (Y axis).
        if (Mathf.Abs(verticalDistance) >= 0.015f)
        {
            float direction = Mathf.Sign(verticalDistance);
            float straight = Mathf.Min(terminalStraightLength,
                Mathf.Abs(verticalDistance) * 0.40f);

            // Stay at channel-bed depth until directly in line with the
            // destination terminal. This creates a 90-degree XY corner.
            AddPoint(points, new Vector3(
                terminalPosition.x, from.y, from.z));

            Vector3 beforeTerminal = new Vector3(
                terminalPosition.x,
                terminalPosition.y - direction * straight,
                from.z);
            AddPoint(points, beforeTerminal);

            // Rise from channel-bed Z to the actual terminal connection Z.
            AddPoint(points, new Vector3(
                beforeTerminal.x, beforeTerminal.y, terminalPosition.z));

            // Last segment is perfectly straight into the terminal.
            AddPoint(points, terminalPosition);
            return;
        }

        // If the terminal is level with the last channel waypoint, make
        // the last visible approach horizontal instead of a tiny Y kink.
        if (Mathf.Abs(horizontalDistance) >= 0.015f)
        {
            float direction = Mathf.Sign(horizontalDistance);
            float straight = Mathf.Min(terminalStraightLength,
                Mathf.Abs(horizontalDistance) * 0.40f);
            Vector3 beforeTerminal = new Vector3(
                terminalPosition.x - direction * straight,
                terminalPosition.y,
                from.z);

            AddPoint(points, new Vector3(
                from.x, terminalPosition.y, from.z));
            AddPoint(points, beforeTerminal);
            AddPoint(points, new Vector3(
                beforeTerminal.x, beforeTerminal.y, terminalPosition.z));
            AddPoint(points, terminalPosition);
            return;
        }

        // Very close terminals: no room for a visible orthogonal exit.
        AddPoint(points, new Vector3(
            terminalPosition.x, terminalPosition.y, from.z));
        AddPoint(points, terminalPosition);
    }

    private void AddStep(List<Vector3> points, Vector3 destination,
        bool horizontalBeforeVertical, bool use45)
    {
        if (points.Count == 0)
        {
            points.Add(destination);
            return;
        }

        Vector3 from = points[points.Count - 1];
        float dx = destination.x - from.x;
        float dy = destination.y - from.y;
        float dz = destination.z - from.z;
        Vector3 flatTarget = new Vector3(destination.x, destination.y, from.z);

        if (Mathf.Abs(dx) > 0.0001f && Mathf.Abs(dy) > 0.0001f)
        {
            Vector3 corner = horizontalBeforeVertical
                ? new Vector3(destination.x, from.y, from.z)
                : new Vector3(from.x, destination.y, from.z);

            if (use45)
            {
                float cut = Mathf.Min(chamferLength,
                    Mathf.Min(Mathf.Abs(dx), Mathf.Abs(dy)) * 0.35f);
                if (cut >= 0.001f)
                {
                    Vector3 before = corner -
                        (corner - from).normalized * cut;
                    Vector3 after = corner +
                        (flatTarget - corner).normalized * cut;
                    AddPoint(points, before);
                    AddPoint(points, after);
                }
                else
                {
                    AddPoint(points, corner);
                }
            }
            else
            {
                AddPoint(points, corner);
            }
        }

        if (Mathf.Abs(dz) < 0.0001f)
        {
            AddPoint(points, destination);
            return;
        }

        // Slope along the final XY approach, rather than a sudden Z-only
        // segment at the terminal or at the channel click.
        Vector3 last = points[points.Count - 1];
        float lastXYLength = Vector2.Distance(
            new Vector2(last.x, last.y),
            new Vector2(destination.x, destination.y));

        if (lastXYLength > 0.003f)
        {
            float radians = entrySlopeAngle * Mathf.Deg2Rad;
            float idealRun = Mathf.Abs(dz) / Mathf.Tan(radians);
            float run = Mathf.Min(idealRun, lastXYLength * maxSlopeFraction);
            if (run > 0.001f)
            {
                Vector3 approach = flatTarget -
                    (flatTarget - last).normalized * run;
                AddPoint(points, approach);
            }
        }

        AddPoint(points, destination);
    }

    private static void AddPoint(List<Vector3> points, Vector3 point)
    {
        if (points.Count == 0 ||
            Vector3.Distance(points[points.Count - 1], point) > 0.0001f)
            points.Add(point);
    }
}
