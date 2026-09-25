using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public sealed class Wire : MonoBehaviour
{
    [SerializeField, Min(0.001f)]
    private float colliderWidth = 0.04f;

    private readonly List<GameObject> segmentObjects =
        new List<GameObject>();

    private LineRenderer lineRenderer;

    private readonly List<Vector3> routePoints =
        new List<Vector3>();

    private Terminal startTerminal;
    private Terminal endTerminal;

    private WireStyle style;

    public Terminal StartTerminal => startTerminal;
    public Terminal EndTerminal => endTerminal;

    public WireStyle Style => style;

    public bool IsCompleted =>
        startTerminal != null &&
        endTerminal != null;

    public int RoutePointCount =>
        routePoints.Count;

    private void Awake()
    {
        lineRenderer =
            GetComponent<LineRenderer>();

        lineRenderer.useWorldSpace = true;
    }

    public void Begin(
        Terminal terminal,
        WireStyle wireStyle)
    {
        if (terminal == null)
            return;

        startTerminal = terminal;
        style = wireStyle;

        ApplyStyle();

        routePoints.Clear();

        Vector3 startPosition =
            startTerminal.ConnectionPosition;

        // İlk nokta başlangıç terminalidir.
        routePoints.Add(startPosition);

        // İkinci nokta hareket eden preview noktasıdır.
        routePoints.Add(startPosition);

        RefreshRenderer();
    }

    public void UpdatePreview(Vector3 position)
    {
        if (routePoints.Count < 2)
            return;

        routePoints[routePoints.Count - 1] =
            position;

        RefreshRenderer();
    }

    public void AddRoutePoint(Vector3 position)
    {
        if (routePoints.Count < 2)
            return;

        // Mevcut preview noktasını sabitliyoruz.
        routePoints[routePoints.Count - 1] =
            position;

        // Aynı noktada yeni bir preview noktası oluşturuyoruz.
        routePoints.Add(position);

        RefreshRenderer();
    }

    public bool Complete(Terminal terminal)
    {
        if (terminal == null)
            return false;

        if (terminal == startTerminal)
            return false;

        if (routePoints.Count < 2)
            return false;

        endTerminal = terminal;

        routePoints[routePoints.Count - 1] =
            endTerminal.ConnectionPosition;

        RefreshRenderer();

        RebuildSegmentColliders();

        return true;
    }

    private void ApplyStyle()
    {
        if (style == null)
        {
            Debug.LogWarning(
                "WireStyle bulunamadı.",
                this
            );

            return;
        }

        lineRenderer.startWidth =
            style.Width;

        lineRenderer.endWidth =
            style.Width;

        lineRenderer.startColor =
            style.WireColor;

        lineRenderer.endColor =
            style.WireColor;

        if (style.Material != null)
        {
            lineRenderer.material =
                new Material(style.Material);

            lineRenderer.material.color =
                style.WireColor;
        }

        Debug.Log(
            $"Wire Style uygulandı | " +
            $"Type: {style.LineType} | " +
            $"Color: {style.WireColor}"
        );
    }



    private void RefreshRenderer()
    {
        lineRenderer.positionCount =
            routePoints.Count;

        lineRenderer.SetPositions(
            routePoints.ToArray()
        );
    }

    private void RebuildSegmentColliders()
    {
        ClearSegmentColliders();

        if (!IsCompleted)
            return;

        if (routePoints.Count < 2)
            return;

        for (int i = 0; i < routePoints.Count - 1; i++)
        {
            Vector3 start =
                routePoints[i];

            Vector3 end =
                routePoints[i + 1];

            CreateSegmentCollider(
                start,
                end,
                i
            );
        }
    }
    private void CreateSegmentCollider(
        Vector3 start,
        Vector3 end,
        int index)
    {
        Vector3 direction =
            end - start;

        float length =
            direction.magnitude;

        if (length <= Mathf.Epsilon)
            return;

        GameObject segment =
            new GameObject(
                $"WireSegment_{index}"
            );

        segment.transform.SetParent(
            transform
        );

        segment.transform.position =
            (start + end) * 0.5f;

        segment.transform.rotation =
            Quaternion.FromToRotation(
                Vector3.forward,
                direction.normalized
            );

        BoxCollider collider =
            segment.AddComponent<BoxCollider>();

        collider.size =
            new Vector3(
                colliderWidth,
                colliderWidth,
                length
            );

        WireSegment wireSegment =
            segment.AddComponent<WireSegment>();

        wireSegment.Initialize(this);

        int wireLayer =
            LayerMask.NameToLayer("Wire");

        if (wireLayer >= 0)
        {
            segment.layer = wireLayer;
        }

        segmentObjects.Add(segment);
    }

    private void ClearSegmentColliders()
    {
        foreach (GameObject segment
                 in segmentObjects)
        {
            if (segment != null)
            {
                Destroy(segment);
            }
        }

        segmentObjects.Clear();
    }
}