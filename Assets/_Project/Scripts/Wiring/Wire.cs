using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public sealed class Wire : MonoBehaviour
{
    [SerializeField, Min(0.001f)] private float colliderWidth = 0.018f;

    private readonly List<GameObject> segmentObjects = new List<GameObject>();
    private readonly List<Vector3> routePoints = new List<Vector3>();

    private LineRenderer lineRenderer;
    private Terminal startTerminal;
    private Terminal endTerminal;
    private WireStyle style;

    public Terminal StartTerminal => startTerminal;
    public Terminal EndTerminal => endTerminal;
    public WireStyle Style => style;
    public bool IsCompleted => startTerminal != null && endTerminal != null;
    public int RoutePointCount => routePoints.Count;
    public IReadOnlyList<Vector3> RoutePoints => routePoints;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.useWorldSpace = true;
    }

    public void Begin(Terminal terminal, WireStyle wireStyle)
    {
        if (terminal == null)
            return;
        startTerminal = terminal;
        style = wireStyle;
        ApplyStyle();
        routePoints.Clear();
        Vector3 startPosition = startTerminal.ConnectionPosition;
        routePoints.Add(startPosition);
        routePoints.Add(startPosition);
        RefreshRenderer();
    }

    // Legacy free-route preview. Guided mode uses SetRoutePreview instead.
    public void UpdatePreview(Vector3 position)
    {
        if (IsCompleted || routePoints.Count < 2)
            return;
        routePoints[routePoints.Count - 1] = position;
        RefreshRenderer();
    }

    public void SetRoutePreview(IReadOnlyList<Vector3> points)
    {
        if (IsCompleted || points == null || points.Count < 2)
            return;
        routePoints.Clear();
        for (int i = 0; i < points.Count; i++)
            routePoints.Add(points[i]);
        RefreshRenderer();
    }

    public void AddRoutePoint(Vector3 position)
    {
        if (IsCompleted || routePoints.Count < 2)
            return;
        routePoints[routePoints.Count - 1] = position;
        routePoints.Add(position);
        RefreshRenderer();
    }

    public bool Complete(Terminal terminal)
    {
        return Complete(terminal, null);
    }

    public bool Complete(Terminal terminal, IReadOnlyList<Vector3> plannedRoute)
    {
        if (terminal == null || terminal == startTerminal || routePoints.Count < 2)
            return false;

        endTerminal = terminal;
        if (plannedRoute != null && plannedRoute.Count >= 2)
        {
            routePoints.Clear();
            for (int i = 0; i < plannedRoute.Count; i++)
                routePoints.Add(plannedRoute[i]);
        }
        else
        {
            routePoints[routePoints.Count - 1] = endTerminal.ConnectionPosition;
        }
        RefreshRenderer();
        RebuildSegmentColliders();
        return true;
    }

    private void ApplyStyle()
    {
        if (style == null)
        {
            Debug.LogWarning("WireStyle bulunamadı.", this);
            return;
        }

        lineRenderer.startWidth = style.Width;
        lineRenderer.endWidth = style.Width;
        lineRenderer.startColor = style.WireColor;
        lineRenderer.endColor = style.WireColor;
        if (style.Material != null)
        {
            lineRenderer.material = new Material(style.Material);
            lineRenderer.material.color = style.WireColor;
        }
        Debug.Log($"Wire Style uygulandı | Type: {style.LineType} | Color: {style.WireColor}");
    }

    private void RefreshRenderer()
    {
        lineRenderer.positionCount = routePoints.Count;
        lineRenderer.SetPositions(routePoints.ToArray());
    }

    private void RebuildSegmentColliders()
    {
        ClearSegmentColliders();
        if (!IsCompleted || routePoints.Count < 2)
            return;

        for (int i = 0; i < routePoints.Count - 1; i++)
            CreateSegmentCollider(routePoints[i], routePoints[i + 1], i);
    }

    private void CreateSegmentCollider(Vector3 start, Vector3 end, int index)
    {
        Vector3 direction = end - start;
        float length = direction.magnitude;
        if (length <= Mathf.Epsilon)
            return;

        GameObject segment = new GameObject($"WireSegment_{index}");
        segment.transform.SetParent(transform);
        segment.transform.position = (start + end) * 0.5f;
        segment.transform.rotation = Quaternion.FromToRotation(
            Vector3.forward, direction.normalized);

        BoxCollider collider = segment.AddComponent<BoxCollider>();
        collider.size = new Vector3(colliderWidth, colliderWidth, length);
        WireSegment wireSegment = segment.AddComponent<WireSegment>();
        wireSegment.Initialize(this);

        int wireLayer = LayerMask.NameToLayer("Wire");
        if (wireLayer >= 0)
            segment.layer = wireLayer;
        segmentObjects.Add(segment);
    }

    private void ClearSegmentColliders()
    {
        foreach (GameObject segment in segmentObjects)
        {
            if (segment != null)
                Destroy(segment);
        }
        segmentObjects.Clear();
    }
}
