using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 3D tubular wire renderer. Keeps the existing Terminal, WireStyle,
/// WireController and WireSegment contract intact.
/// Route points are stored in world coordinates; only the rendering geometry
/// is rounded. Electrical connections and collision segments use the original
/// route points.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public sealed class Wire : MonoBehaviour
{
    [Header("Existing collision / interaction")]
    [SerializeField, Min(0.001f)] private float colliderWidth = 0.018f;

    [Header("3D tubular appearance")]
    [SerializeField, Range(6, 24)] private int radialSegments = 12;
    [SerializeField] private bool roundCorners = true;
    [SerializeField, Min(0.001f)] private float cornerRadius = 0.018f;
    [SerializeField, Range(1, 8)] private int bendSubdivisions = 3;
    [SerializeField] private bool capEnds = true;

    [Header("Route cleanup")]
    [Tooltip("Removes unnecessary reverse runs when a cable waypoint passes the terminal alignment.")]
    [SerializeField] private bool removeOverlappingFoldbacks = true;

    private readonly List<GameObject> segmentObjects = new List<GameObject>();
    private readonly List<Vector3> routePoints = new List<Vector3>();
    private readonly List<Vector3> visiblePoints = new List<Vector3>();
    private readonly List<Vector3> vertices = new List<Vector3>();
    private readonly List<Vector3> normals = new List<Vector3>();
    private readonly List<Vector2> uvs = new List<Vector2>();
    private readonly List<int> triangles = new List<int>();

    private LineRenderer lineRenderer;
    private MeshFilter tubeFilter;
    private MeshRenderer tubeRenderer;
    private Mesh tubeMesh;
    private MaterialPropertyBlock propertyBlock;

    private Terminal startTerminal;
    private Terminal endTerminal;
    private WireStyle style;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private const float MinimumPointDistance = 0.0005f;

    public Terminal StartTerminal => startTerminal;
    public Terminal EndTerminal => endTerminal;
    public WireStyle Style => style;
    public bool IsCompleted => startTerminal != null && endTerminal != null;
    public int RoutePointCount => routePoints.Count;
    public IReadOnlyList<Vector3> RoutePoints => routePoints;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        // A LineRenderer is always a ribbon, even with TransformZ alignment.
        // It is kept as a disabled component for prefab/script compatibility.
        lineRenderer.enabled = false;
        lineRenderer.useWorldSpace = true;

        GameObject tubeObject = new GameObject("WireTube_3D");
        tubeObject.transform.SetParent(transform, false);
        tubeObject.transform.localPosition = Vector3.zero;
        tubeObject.transform.localRotation = Quaternion.identity;
        tubeObject.transform.localScale = Vector3.one;

        tubeFilter = tubeObject.AddComponent<MeshFilter>();
        tubeRenderer = tubeObject.AddComponent<MeshRenderer>();
        tubeRenderer.shadowCastingMode = ShadowCastingMode.Off;
        tubeRenderer.receiveShadows = true;
        tubeRenderer.lightProbeUsage = LightProbeUsage.BlendProbes;

        tubeMesh = new Mesh { name = "RuntimeWireTube" };
        tubeMesh.MarkDynamic();
        tubeFilter.sharedMesh = tubeMesh;
        propertyBlock = new MaterialPropertyBlock();
    }

    private void OnEnable()
    {
        if (lineRenderer != null)
            lineRenderer.enabled = false;
    }

    private void OnDestroy()
    {
        if (tubeMesh != null)
        {
            Destroy(tubeMesh);
            tubeMesh = null;
        }
    }

    public void Begin(Terminal terminal, WireStyle wireStyle)
    {
        if (terminal == null)
            return;

        startTerminal = terminal;
        endTerminal = null;
        style = wireStyle;
        ApplyStyle();

        routePoints.Clear();
        Vector3 position = startTerminal.ConnectionPosition;
        routePoints.Add(position);
        routePoints.Add(position);
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

        // The manual router may run beyond the terminal and then retrace
        // the same channel line. Remove this overlap from the committed path
        // so that its colliders and electrical route geometry also agree.
        NormalizeCompletedRoute();
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

        // Reuse the existing project material without cloning it per wire.
        // Color is individual for each wire via MaterialPropertyBlock.
        tubeRenderer.sharedMaterial = style.Material;
        propertyBlock.Clear();
        propertyBlock.SetColor(BaseColorId, style.WireColor);
        propertyBlock.SetColor(ColorId, style.WireColor);
        tubeRenderer.SetPropertyBlock(propertyBlock);

        if (style.Material == null)
            Debug.LogWarning("WireStyle Material boş. MAT_Wire atanmalı.", this);
    }

    private void RefreshRenderer()
    {
        if (tubeMesh == null)
            return;

        BuildVisibleRoute();
        RebuildTubeMesh();
    }

    private void BuildVisibleRoute()
    {
        visiblePoints.Clear();
        if (routePoints.Count == 0)
            return;

        // This works for free, guided and automatic routes. During preview
        // routePoints remain untouched; only visible geometry is cleaned.
        // On Complete(), the exact same cleanup is committed to routePoints.
        List<Vector3> clean = new List<Vector3>(routePoints.Count);
        BuildCleanRoute(routePoints, clean);

        if (clean.Count < 2)
            return;

        visiblePoints.Add(clean[0]);
        if (roundCorners)
        {
            for (int i = 1; i < clean.Count - 1; i++)
            {
                Vector3 prev = clean[i - 1];
                Vector3 current = clean[i];
                Vector3 next = clean[i + 1];
                Vector3 inVector = current - prev;
                Vector3 outVector = next - current;
                float inLen = inVector.magnitude;
                float outLen = outVector.magnitude;
                if (inLen <= MinimumPointDistance || outLen <= MinimumPointDistance)
                    continue;

                Vector3 incoming = inVector / inLen;
                Vector3 outgoing = outVector / outLen;
                float dot = Vector3.Dot(incoming, outgoing);

                // Avoid modifying almost straight points or extremely sharp U-turns.
                if (dot > 0.995f || dot < -0.95f)
                {
                    AddVisiblePoint(current);
                    continue;
                }

                float cut = Mathf.Min(cornerRadius, inLen * 0.35f, outLen * 0.35f);
                if (cut <= MinimumPointDistance)
                {
                    AddVisiblePoint(current);
                    continue;
                }

                Vector3 enter = current - incoming * cut;
                Vector3 exit = current + outgoing * cut;
                AddVisiblePoint(enter);

                // Quadratic Bezier bend, tangent to adjacent straight portions.
                for (int j = 1; j < bendSubdivisions; j++)
                {
                    float t = j / (float)bendSubdivisions;
                    Vector3 bend = (1f - t) * (1f - t) * enter +
                                   2f * (1f - t) * t * current +
                                   t * t * exit;
                    AddVisiblePoint(bend);
                }
                AddVisiblePoint(exit);
            }
        }
        else
        {
            for (int i = 1; i < clean.Count - 1; i++)
                AddVisiblePoint(clean[i]);
        }
        AddVisiblePoint(clean[clean.Count - 1]);
    }

    /// <summary>
    /// Removes a retraced segment when the user-selected channel waypoint
    /// lies past the destination terminal. For example:
    ///   [0, 0] -> [2, 0] -> [1, 0]  becomes  [0, 0] -> [1, 0].
    /// This is purely geometric: terminals and connection validation are
    /// never changed. Non-collinear intentional turns are preserved.
    /// </summary>
    private void BuildCleanRoute(IReadOnlyList<Vector3> source,
        List<Vector3> destination)
    {
        destination.Clear();
        if (source == null)
            return;

        float diameter = style != null ? style.Width : 0.015f;
        float alignmentTolerance = Mathf.Max(0.002f, diameter * 0.65f);

        for (int i = 0; i < source.Count; i++)
        {
            Vector3 point = source[i];
            if (destination.Count > 0 &&
                Vector3.Distance(destination[destination.Count - 1], point)
                    <= MinimumPointDistance)
                continue;

            if (removeOverlappingFoldbacks)
            {
                while (destination.Count >= 2)
                {
                    Vector3 a = destination[destination.Count - 2];
                    Vector3 b = destination[destination.Count - 1];
                    if (!IsRetracedSegment(a, b, point, alignmentTolerance))
                        break;

                    // b is an unnecessary overshoot. The shortened path
                    // goes directly from a toward the new point instead.
                    destination.RemoveAt(destination.Count - 1);
                }
            }

            if (destination.Count == 0 ||
                Vector3.Distance(destination[destination.Count - 1], point)
                    > MinimumPointDistance)
                destination.Add(point);
        }
    }

    private static bool IsRetracedSegment(Vector3 a, Vector3 b, Vector3 c,
        float tolerance)
    {
        Vector3 forward = b - a;
        Vector3 returning = c - b;
        float forwardLength = forward.magnitude;
        float returnLength = returning.magnitude;
        if (forwardLength < MinimumPointDistance ||
            returnLength < MinimumPointDistance)
            return false;

        // Backwards movement must be almost opposite to the last leg.
        // This also tolerates small XY/Z offsets from the routing surface.
        float alignment = Vector3.Dot(forward, returning) /
                          (forwardLength * returnLength);
        if (alignment > -0.94f)
            return false;

        // Do not erase a deliberate turn into a neighboring channel.
        // The end must still be near the incoming leg's infinite line.
        float lateralOffset = Vector3.Cross(c - a, forward).magnitude /
                              forwardLength;
        return lateralOffset <= tolerance;
    }

    private void NormalizeCompletedRoute()
    {
        if (!removeOverlappingFoldbacks || routePoints.Count < 3)
            return;

        List<Vector3> cleaned = new List<Vector3>(routePoints.Count);
        BuildCleanRoute(routePoints, cleaned);
        if (cleaned.Count < 2)
            return;

        routePoints.Clear();
        routePoints.AddRange(cleaned);
    }

    private void AddVisiblePoint(Vector3 point)
    {
        if (visiblePoints.Count == 0 ||
            Vector3.Distance(visiblePoints[visiblePoints.Count - 1], point) > 0.00001f)
            visiblePoints.Add(point);
    }

    private void RebuildTubeMesh()
    {
        tubeMesh.Clear();
        vertices.Clear();
        normals.Clear();
        uvs.Clear();
        triangles.Clear();

        int count = visiblePoints.Count;
        if (count < 2)
            return;

        int sides = Mathf.Clamp(radialSegments, 6, 24);
        float diameter = style != null ? style.Width : 0.015f;
        float radius = Mathf.Max(0.0005f, diameter * 0.5f);

        Vector3 previousNormal = Vector3.zero;
        float distanceAlong = 0f;
        for (int i = 0; i < count; i++)
        {
            Vector3 tangent;
            if (i == 0)
                tangent = (visiblePoints[1] - visiblePoints[0]).normalized;
            else if (i == count - 1)
                tangent = (visiblePoints[i] - visiblePoints[i - 1]).normalized;
            else
            {
                Vector3 inDirection = (visiblePoints[i] - visiblePoints[i - 1]).normalized;
                Vector3 outDirection = (visiblePoints[i + 1] - visiblePoints[i]).normalized;
                // A nearly 180-degree turn has no reliable averaged tangent.
                // Use the incoming direction instead of a sudden arbitrary
                // forward axis, which could twist the tube's cross-section.
                tangent = Vector3.Dot(inDirection, outDirection) < -0.94f
                    ? inDirection
                    : (visiblePoints[i + 1] - visiblePoints[i - 1]).normalized;
            }

            if (tangent.sqrMagnitude < 0.000001f)
                tangent = Vector3.forward;

            Vector3 ringNormal;
            if (i == 0)
            {
                Vector3 reference = Mathf.Abs(Vector3.Dot(tangent, Vector3.up)) > 0.9f
                    ? Vector3.right : Vector3.up;
                ringNormal = Vector3.Cross(tangent, reference).normalized;
            }
            else
            {
                // Transport the cross-section without flipping at vertical bends.
                ringNormal = previousNormal - tangent * Vector3.Dot(previousNormal, tangent);
                if (ringNormal.sqrMagnitude < 0.000001f)
                    ringNormal = Vector3.Cross(tangent,
                        Mathf.Abs(Vector3.Dot(tangent, Vector3.up)) > 0.9f
                            ? Vector3.right : Vector3.up);
                ringNormal.Normalize();
            }

            Vector3 ringBinormal = Vector3.Cross(tangent, ringNormal).normalized;
            previousNormal = ringNormal;

            if (i > 0)
                distanceAlong += Vector3.Distance(visiblePoints[i - 1], visiblePoints[i]);

            for (int j = 0; j <= sides; j++)
            {
                float angle = j / (float)sides * Mathf.PI * 2f;
                Vector3 outward = ringNormal * Mathf.Cos(angle) +
                                  ringBinormal * Mathf.Sin(angle);
                Vector3 worldPosition = visiblePoints[i] + outward * radius;

                vertices.Add(transform.InverseTransformPoint(worldPosition));
                normals.Add(transform.InverseTransformDirection(outward).normalized);
                uvs.Add(new Vector2(j / (float)sides, distanceAlong));
            }
        }

        int ring = sides + 1;
        for (int i = 0; i < count - 1; i++)
        {
            for (int j = 0; j < sides; j++)
            {
                int a = i * ring + j;
                int b = a + 1;
                int c = (i + 1) * ring + j;
                int d = c + 1;
                // Outward winding for tangent x radial cross-section.
                triangles.Add(a); triangles.Add(b); triangles.Add(c);
                triangles.Add(b); triangles.Add(d); triangles.Add(c);
            }
        }

        if (capEnds)
        {
            AddEndCap(visiblePoints[0],
                -(visiblePoints[1] - visiblePoints[0]).normalized,
                0, sides, true);
            AddEndCap(visiblePoints[count - 1],
                (visiblePoints[count - 1] - visiblePoints[count - 2]).normalized,
                (count - 1) * ring, sides, false);
        }

        tubeMesh.SetVertices(vertices);
        tubeMesh.SetNormals(normals);
        tubeMesh.SetUVs(0, uvs);
        tubeMesh.SetTriangles(triangles, 0);
        tubeMesh.RecalculateBounds();
    }

    private void AddEndCap(Vector3 center, Vector3 normal,
        int ringStartIndex, int sides, bool isStart)
    {
        Vector3 localNormal = transform.InverseTransformDirection(normal).normalized;
        int centerIndex = vertices.Count;
        vertices.Add(transform.InverseTransformPoint(center));
        normals.Add(localNormal);
        uvs.Add(new Vector2(0.5f, 0.5f));

        // Duplicate ring vertices for flat end-cap lighting.
        int capRing = vertices.Count;
        for (int j = 0; j <= sides; j++)
        {
            vertices.Add(vertices[ringStartIndex + j]);
            normals.Add(localNormal);
            uvs.Add(new Vector2(0.5f, 0.5f));
        }

        for (int j = 0; j < sides; j++)
        {
            if (isStart)
            {
                triangles.Add(centerIndex);
                triangles.Add(capRing + j + 1);
                triangles.Add(capRing + j);
            }
            else
            {
                triangles.Add(centerIndex);
                triangles.Add(capRing + j);
                triangles.Add(capRing + j + 1);
            }
        }
    }

    // Keep existing mouse/raycast-based deletion and interaction unchanged.
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
        float effectiveWidth = Mathf.Max(colliderWidth,
            style != null ? style.Width : colliderWidth);
        collider.size = new Vector3(effectiveWidth, effectiveWidth, length);
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
