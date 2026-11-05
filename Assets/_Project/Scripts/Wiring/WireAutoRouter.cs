using System.Collections.Generic;
using UnityEngine;

public enum WireBendMode
{
    RightAngle90,
    Beveled45
}

/// <summary>
/// Creates a channel-preferred 2.5D route without changing the electrical connection.
/// First iteration: parallel channel lanes and 90/45 degree bends.
/// A full obstacle-aware channel graph is a separate future iteration.
/// </summary>
public sealed class WireAutoRouter : MonoBehaviour
{
    [Header("Bends")]
    [SerializeField] private WireBendMode bendMode = WireBendMode.RightAngle90;
    [SerializeField, Min(0.005f)] private float terminalStraightLength = 0.045f;
    [SerializeField, Min(0.005f)] private float chamferLength = 0.025f;

    [Header("Cable tray lanes")]
    [SerializeField, Min(0.005f)] private float laneSpacing = 0.020f;
    [SerializeField, Range(1, 5)] private int lanesPerChannel = 3;
    [SerializeField, Min(0.001f)] private float frontOffset = 0.025f;
    [SerializeField, Min(0.001f)] private float depthLayerSpacing = 0.020f;
    [SerializeField] private bool frontIsNegativeZ = true;

    private struct Channel
    {
        public bool Horizontal;
        public float Axis;
        public float Min;
        public float Max;
        public float HalfWidth;
    }

    public List<Vector3> BuildRoute(
        Terminal source,
        Terminal destination,
        IReadOnlyList<Connection> existingConnections)
    {
        if (source == null || destination == null || source == destination)
            return null;

        Vector3 a = source.ConnectionPosition;
        Vector3 b = destination.ConnectionPosition;
        List<Channel> channels = CollectChannels();

        if (channels.Count == 0)
        {
            channels.Add(new Channel
            {
                Horizontal = true,
                Axis = (a.y + b.y) * 0.5f,
                Min = Mathf.Min(a.x, b.x),
                Max = Mathf.Max(a.x, b.x),
                HalfWidth = laneSpacing * 1.5f
            });
        }

        float bestScore = float.PositiveInfinity;
        Channel bestChannel = channels[0];
        float bestAxis = bestChannel.Axis;
        int bestOverlapCount = 0;

        foreach (Channel channel in channels)
        {
            int laneCount = Mathf.Max(1, lanesPerChannel);
            int centerLane = laneCount / 2;

            for (int i = 0; i < laneCount; i++)
            {
                float offset = (i - centerLane) * laneSpacing;
                if (Mathf.Abs(offset) + laneSpacing * 0.5f >
                    channel.HalfWidth + 0.002f)
                    continue;

                float axis = channel.Axis + offset;
                int overlap = CountParallelOverlaps(
                    channel.Horizontal, axis, a, b, existingConnections);

                float score = ScoreChannel(channel, a, b) +
                              Mathf.Abs(offset) * 0.05f + overlap * 10f;

                if (score >= bestScore)
                    continue;

                bestScore = score;
                bestChannel = channel;
                bestAxis = axis;
                bestOverlapCount = overlap;
            }
        }

        // Very narrow channels may have no usable lane under current settings.
        if (float.IsPositiveInfinity(bestScore))
        {
            bestChannel = channels[0];
            bestAxis = bestChannel.Axis;
            bestOverlapCount = CountParallelOverlaps(
                bestChannel.Horizontal, bestAxis, a, b, existingConnections);
        }

        float direction = frontIsNegativeZ ? -1f : 1f;
        float surfaceZ = frontIsNegativeZ
            ? Mathf.Min(a.z, b.z)
            : Mathf.Max(a.z, b.z);
        float channelZ = surfaceZ + direction *
            (frontOffset + bestOverlapCount * depthLayerSpacing);

        List<Vector3> route = bestChannel.Horizontal
            ? BuildHorizontalRoute(a, b, bestAxis, channelZ)
            : BuildVerticalRoute(a, b, bestAxis, channelZ);

        route = RemoveRedundantPoints(route);

        if (bendMode == WireBendMode.Beveled45)
            route = Add45DegreeCorners(route);

        return route;
    }

    private List<Channel> CollectChannels()
    {
        List<Channel> result = new List<Channel>();
        WireSurface[] surfaces = FindObjectsByType<WireSurface>();

        foreach (WireSurface surface in surfaces)
        {
            if (surface == null || !surface.isActiveAndEnabled)
                continue;

            BoxCollider box = surface.GetComponent<BoxCollider>();
            if (box == null || !box.enabled)
                continue;

            Bounds bounds = box.bounds;
            bool horizontal = bounds.size.x >= bounds.size.y;
            Channel next = new Channel
            {
                Horizontal = horizontal,
                Axis = horizontal ? bounds.center.y : bounds.center.x,
                Min = horizontal ? bounds.min.x : bounds.min.y,
                Max = horizontal ? bounds.max.x : bounds.max.y,
                HalfWidth = Mathf.Min(bounds.size.x, bounds.size.y) * 0.5f
            };

            bool merged = false;
            for (int i = 0; i < result.Count; i++)
            {
                Channel previous = result[i];
                if (previous.Horizontal != next.Horizontal ||
                    Mathf.Abs(previous.Axis - next.Axis) > 0.035f)
                    continue;

                // Adjacent tray pieces are treated as a single lane corridor.
                previous.Min = Mathf.Min(previous.Min, next.Min);
                previous.Max = Mathf.Max(previous.Max, next.Max);
                previous.HalfWidth = Mathf.Min(previous.HalfWidth, next.HalfWidth);
                result[i] = previous;
                merged = true;
                break;
            }

            if (!merged)
                result.Add(next);
        }

        return result;
    }

    private static float ScoreChannel(Channel channel, Vector3 a, Vector3 b)
    {
        if (channel.Horizontal)
        {
            return Mathf.Abs(a.y - channel.Axis) +
                   Mathf.Abs(b.y - channel.Axis) +
                   3f * (Outside(a.x, channel.Min, channel.Max) +
                         Outside(b.x, channel.Min, channel.Max));
        }

        return Mathf.Abs(a.x - channel.Axis) +
               Mathf.Abs(b.x - channel.Axis) +
               3f * (Outside(a.y, channel.Min, channel.Max) +
                     Outside(b.y, channel.Min, channel.Max));
    }

    private static float Outside(float value, float min, float max)
    {
        return value < min ? min - value :
               value > max ? value - max : 0f;
    }

    private int CountParallelOverlaps(
        bool horizontal,
        float axis,
        Vector3 a,
        Vector3 b,
        IReadOnlyList<Connection> existingConnections)
    {
        if (existingConnections == null)
            return 0;

        float from = horizontal ? Mathf.Min(a.x, b.x) : Mathf.Min(a.y, b.y);
        float to = horizontal ? Mathf.Max(a.x, b.x) : Mathf.Max(a.y, b.y);
        int overlaps = 0;

        foreach (Connection connection in existingConnections)
        {
            if (connection == null || connection.Wire == null)
                continue;

            IReadOnlyList<Vector3> points = connection.Wire.RoutePoints;
            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector3 p = points[i];
                Vector3 q = points[i + 1];
                float pAxis = horizontal ? p.y : p.x;
                float qAxis = horizontal ? q.y : q.x;

                if (Mathf.Abs(pAxis - axis) > laneSpacing * 0.8f ||
                    Mathf.Abs(qAxis - axis) > laneSpacing * 0.8f)
                    continue;

                float pAlong = horizontal ? p.x : p.y;
                float qAlong = horizontal ? q.x : q.y;
                float overlapLength = Mathf.Min(to, Mathf.Max(pAlong, qAlong)) -
                                      Mathf.Max(from, Mathf.Min(pAlong, qAlong));
                if (overlapLength > 0.015f)
                {
                    overlaps++;
                    break;
                }
            }
        }

        return overlaps;
    }

    private List<Vector3> BuildHorizontalRoute(
        Vector3 a, Vector3 b, float channelY, float channelZ)
    {
        Vector3 aStub = HorizontalStub(a, channelY, b.x);
        Vector3 bStub = HorizontalStub(b, channelY, a.x);

        return new List<Vector3>
        {
            a,
            aStub,
            new Vector3(aStub.x, aStub.y, channelZ),
            new Vector3(aStub.x, channelY, channelZ),
            new Vector3(bStub.x, channelY, channelZ),
            new Vector3(bStub.x, bStub.y, channelZ),
            bStub,
            b
        };
    }

    private List<Vector3> BuildVerticalRoute(
        Vector3 a, Vector3 b, float channelX, float channelZ)
    {
        Vector3 aStub = VerticalStub(a, channelX, b.y);
        Vector3 bStub = VerticalStub(b, channelX, a.y);

        return new List<Vector3>
        {
            a,
            aStub,
            new Vector3(aStub.x, aStub.y, channelZ),
            new Vector3(channelX, aStub.y, channelZ),
            new Vector3(channelX, bStub.y, channelZ),
            new Vector3(bStub.x, bStub.y, channelZ),
            bStub,
            b
        };
    }

    private Vector3 HorizontalStub(Vector3 point, float channelY, float towardX)
    {
        if (Mathf.Abs(channelY - point.y) < 0.01f)
        {
            float direction = towardX < point.x ? -1f : 1f;
            return point + Vector3.right * direction * terminalStraightLength;
        }
        return point + Vector3.up * StubDelta(channelY - point.y);
    }

    private Vector3 VerticalStub(Vector3 point, float channelX, float towardY)
    {
        if (Mathf.Abs(channelX - point.x) < 0.01f)
        {
            float direction = towardY < point.y ? -1f : 1f;
            return point + Vector3.up * direction * terminalStraightLength;
        }
        return point + Vector3.right * StubDelta(channelX - point.x);
    }

    private float StubDelta(float delta)
    {
        float sign = delta < 0f ? -1f : 1f;
        float length = Mathf.Min(terminalStraightLength,
            Mathf.Max(0.005f, Mathf.Abs(delta) * 0.4f));
        return sign * length;
    }

    private static List<Vector3> RemoveRedundantPoints(List<Vector3> points)
    {
        List<Vector3> cleaned = new List<Vector3>();
        foreach (Vector3 p in points)
        {
            if (cleaned.Count > 0 &&
                Vector3.Distance(cleaned[cleaned.Count - 1], p) < 0.0005f)
                continue;

            while (cleaned.Count >= 2)
            {
                Vector3 before = cleaned[cleaned.Count - 2];
                Vector3 middle = cleaned[cleaned.Count - 1];
                Vector3 d1 = middle - before;
                Vector3 d2 = p - middle;
                if (d1.sqrMagnitude < 0.0000001f ||
                    d2.sqrMagnitude < 0.0000001f)
                    break;

                if (Vector3.Cross(d1.normalized, d2.normalized).sqrMagnitude >
                    0.000001f || Vector3.Dot(d1, d2) <= 0f)
                    break;

                cleaned.RemoveAt(cleaned.Count - 1);
            }

            cleaned.Add(p);
        }
        return cleaned;
    }

    private List<Vector3> Add45DegreeCorners(List<Vector3> points)
    {
        if (points.Count < 3)
            return points;

        List<Vector3> result = new List<Vector3> { points[0] };
        for (int i = 1; i < points.Count - 1; i++)
        {
            Vector3 incoming = points[i] - points[i - 1];
            Vector3 outgoing = points[i + 1] - points[i];
            bool inPlane = Mathf.Abs(incoming.z) < 0.0001f &&
                           Mathf.Abs(outgoing.z) < 0.0001f;
            bool rightAngle = Mathf.Abs(Vector3.Dot(
                incoming.normalized, outgoing.normalized)) < 0.01f;

            if (inPlane && rightAngle)
            {
                float cut = Mathf.Min(chamferLength,
                    Mathf.Min(incoming.magnitude, outgoing.magnitude) * 0.33f);
                if (cut >= 0.002f)
                {
                    result.Add(points[i] - incoming.normalized * cut);
                    result.Add(points[i] + outgoing.normalized * cut);
                    continue;
                }
            }

            result.Add(points[i]);
        }

        result.Add(points[points.Count - 1]);
        return RemoveRedundantPoints(result);
    }
}
