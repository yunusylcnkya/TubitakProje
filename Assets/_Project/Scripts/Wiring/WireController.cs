using System.Collections.Generic;
using UnityEngine;

public sealed class WireController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private TerminalSelector terminalSelector;
    [SerializeField] private EquipmentHolder equipmentHolder;
    [SerializeField] private ConnectionManager connectionManager;
    [SerializeField] private ConnectionFeedbackUI feedbackUI;

    [Header("Wire")]
    [SerializeField] private Wire wirePrefab;
    [SerializeField] private WireStyleLibrary styleLibrary;

    [Header("Route Settings")]
    [SerializeField, Min(0.1f)] private float routeDistance = 5f;
    [SerializeField] private LayerMask wireSurfaceMask;

    [Header("Automatic Routing - optional")]
    [SerializeField] private WireAutoRouter autoRouter;
    [SerializeField] private bool useAutoRouting = false;

    [Header("Guided Manual Routing - recommended")]
    [SerializeField] private WireManualRouter manualRouter;
    [SerializeField] private bool useGuidedManualRouting = true;

    private Terminal startTerminal;
    private Wire activeWire;
    private Terminal previewTerminal;
    private List<Vector3> previewRoute;
    private readonly List<ManualWireAnchor> manualAnchors = new List<ManualWireAnchor>();

    public Terminal StartTerminal => startTerminal;
    public bool IsWiring => activeWire != null;
    public bool HasValidRouteSurface { get; private set; }

    private bool AutoRoutingEnabled => useAutoRouting && autoRouter != null;
    private bool GuidedManualEnabled => !AutoRoutingEnabled &&
        useGuidedManualRouting && manualRouter != null;

    private void Awake()
    {
        if (autoRouter == null)
            autoRouter = GetComponent<WireAutoRouter>();
        if (manualRouter == null)
            manualRouter = GetComponent<WireManualRouter>();
    }

    private void Update()
    {
        if (IsWiring)
            UpdateWirePreview();
        HandleInput();
    }

    private void HandleInput()
    {
        if (equipmentHolder != null && equipmentHolder.HasEquipment)
            return;

        if (IsWiring && GuidedManualEnabled &&
            Input.GetKeyDown(KeyCode.Backspace))
        {
            UndoLastWaypoint();
            return;
        }

        if (Input.GetMouseButtonDown(0))
            HandleLeftClick();

        if (Input.GetMouseButtonDown(1))
            CancelWire();
    }

    private void HandleLeftClick()
    {
        Terminal terminal = terminalSelector != null
            ? terminalSelector.CurrentTerminal : null;

        if (!IsWiring)
        {
            TryStartWire(terminal);
            return;
        }

        if (terminal != null)
        {
            TryCompleteWire(terminal);
            return;
        }

        TryAddRoutePoint();
    }

    private void TryStartWire(Terminal terminal)
    {
        if (terminal == null)
            return;

        if (!terminal.IsAvailableForWiring)
        {
            Debug.Log($"Terminal kullanılamıyor: {terminal.DisplayName}");
            return;
        }

        if (wirePrefab == null || styleLibrary == null || connectionManager == null)
        {
            Debug.LogError("WireController: Wire Prefab, Style Library veya ConnectionManager eksik.");
            return;
        }

        WireStyle style = styleLibrary.GetStyle(terminal.LineType);
        if (style == null)
        {
            Debug.LogError($"WireStyle bulunamadı: {terminal.LineType}");
            return;
        }

        startTerminal = terminal;
        activeWire = Instantiate(wirePrefab);
        activeWire.Begin(startTerminal, style);

        previewTerminal = null;
        previewRoute = null;
        manualAnchors.Clear();

        Debug.Log($"Kablo başladı | Terminal: {startTerminal.TerminalId} | Hat: {startTerminal.LineType}");
    }

    private void TryAddRoutePoint()
    {
        if (!IsWiring || AutoRoutingEnabled)
            return;

        if (!TryGetRouteSurface(out RaycastHit hit, out WireSurface surface))
        {
            Debug.Log("Bu yüzeye kablo noktası eklenemez.");
            return;
        }

        if (GuidedManualEnabled)
        {
            if (!manualRouter.TryCreateAnchor(surface, hit.point, startTerminal,
                connectionManager.Connections, manualAnchors, out ManualWireAnchor anchor))
            {
                Debug.LogWarning("Kanalda uygun boş kablo şeridi yok veya WireSurface/BoxCollider eksik.");
                return;
            }

            if (manualAnchors.Count > 0 &&
                Vector3.Distance(manualAnchors[manualAnchors.Count - 1].Position,
                    anchor.Position) < 0.005f)
                return;

            manualAnchors.Add(anchor);
            activeWire.SetRoutePreview(
                manualRouter.BuildPreview(startTerminal, manualAnchors, null));
            Debug.Log($"Manuel güzergâh noktası {manualAnchors.Count}: {surface.name}");
            return;
        }

        // Legacy free-point mode: kept for backward compatibility.
        activeWire.AddRoutePoint(hit.point);
        Debug.Log($"Serbest kablo noktası eklendi: {surface.name}");
    }

    private void TryCompleteWire(Terminal targetTerminal)
    {
        if (!IsWiring || targetTerminal == null)
            return;

        if (targetTerminal == startTerminal)
        {
            Debug.Log("Kablo aynı terminale bağlanamaz.");
            return;
        }

        if (!targetTerminal.IsAvailableForWiring)
        {
            ShowFeedback(new ConnectionValidationResult(
                ConnectionIssueSeverity.Warning,
                $"'{targetTerminal.DisplayName}' terminali kullanılamıyor. " +
                "Bağlantı kapasitesini ve ekipmanın yerleşim durumunu kontrol edin.",
                false));
            return;
        }

        if (GuidedManualEnabled && manualAnchors.Count == 0)
        {
            Debug.LogWarning("Önce kablo kanalına en az bir kez tıklayarak güzergâh seçin.");
            return;
        }

        ConnectionValidationResult validation = connectionManager.ValidateConnection(
            startTerminal, targetTerminal);

        Debug.Log($"[WIRE VALIDATION] Start: {startTerminal.LineType} | " +
            $"Target: {targetTerminal.LineType} | Severity: {validation.Severity} | " +
            $"Allowed: {validation.IsAllowed} | Message: {validation.Message}", this);

        if (!validation.IsAllowed)
        {
            ShowFeedback(validation);
            Debug.LogWarning($"Bağlantı engellendi: {validation.Message}");
            return;
        }

        ElectricalSimulationSystem simulation = connectionManager.ElectricalSimulation;
        bool hadShortCircuit = simulation != null &&
            simulation.LastSnapshot != null && simulation.LastSnapshot.HasShortCircuit;

        IReadOnlyList<Vector3> plannedRoute = null;
        if (AutoRoutingEnabled)
        {
            plannedRoute = targetTerminal == previewTerminal && previewRoute != null
                ? previewRoute
                : autoRouter.BuildRoute(startTerminal, targetTerminal,
                    connectionManager.Connections);
        }
        else if (GuidedManualEnabled)
        {
            plannedRoute = manualRouter.BuildCompletionRoute(
                startTerminal, targetTerminal, manualAnchors);
            if (plannedRoute == null || plannedRoute.Count < 2)
            {
                Debug.LogWarning("Manuel kablo güzergâhı oluşturulamadı.");
                return;
            }
        }

        if (!activeWire.Complete(targetTerminal, plannedRoute))
        {
            Debug.LogWarning("Wire tamamlanamadı.");
            return;
        }

        bool startConnected = startTerminal.TryConnect(activeWire);
        bool endConnected = targetTerminal.TryConnect(activeWire);
        if (!startConnected || !endConnected)
        {
            RollbackConnection(targetTerminal, startConnected, endConnected);
            Debug.LogWarning("Terminal bağlantısı tamamlanamadı.");
            return;
        }

        bool registered = connectionManager.RegisterConnection(
            startTerminal, targetTerminal, activeWire);
        if (!registered)
        {
            RollbackConnection(targetTerminal, true, true);
            Debug.LogWarning("ConnectionManager bağlantıyı kaydetmedi.");
            return;
        }

        Debug.Log($"Bağlantı tamamlandı | {startTerminal.TerminalId} -> " +
            $"{targetTerminal.TerminalId}");

        bool newShortCircuit = simulation != null &&
            simulation.LastSnapshot != null && simulation.LastSnapshot.HasShortCircuit;

        if (!hadShortCircuit && newShortCircuit)
        {
            string message = validation.Severity == ConnectionIssueSeverity.Danger
                ? validation.Message + " Simülasyon kısa devre algıladı."
                : "Yeni bağlantı sonrasında simülasyon kısa devre algıladı.";
            ShowFeedback(new ConnectionValidationResult(
                ConnectionIssueSeverity.Danger, message, true));
        }
        else
        {
            ShowFeedback(validation);
        }

        ResetWiringState();
    }

    private void RollbackConnection(Terminal targetTerminal,
        bool startConnected, bool endConnected)
    {
        if (activeWire != null)
        {
            if (startConnected && startTerminal != null)
                startTerminal.Disconnect(activeWire);
            if (endConnected && targetTerminal != null)
                targetTerminal.Disconnect(activeWire);
            Destroy(activeWire.gameObject);
        }
        ResetWiringState();
    }

    private void ShowFeedback(ConnectionValidationResult result)
    {
        if (result.Severity == ConnectionIssueSeverity.None)
            return;
        if (feedbackUI != null)
            feedbackUI.Show(result);
        else
            Debug.LogWarning(result.Message);
    }

    private void UpdateWirePreview()
    {
        if (!IsWiring)
            return;

        HasValidRouteSurface = false;
        Terminal terminal = terminalSelector != null
            ? terminalSelector.CurrentTerminal : null;

        if (terminal != null && terminal != startTerminal)
        {
            if (AutoRoutingEnabled)
            {
                if (previewTerminal != terminal)
                {
                    previewRoute = autoRouter.BuildRoute(startTerminal,
                        terminal, connectionManager.Connections);
                    previewTerminal = terminal;
                }
                if (previewRoute != null)
                    activeWire.SetRoutePreview(previewRoute);
            }
            else if (GuidedManualEnabled)
            {
                List<Vector3> path = manualRouter.BuildCompletionRoute(
                    startTerminal, terminal, manualAnchors);
                if (path != null)
                    activeWire.SetRoutePreview(path);
            }
            else
            {
                activeWire.UpdatePreview(terminal.ConnectionPosition);
            }
            return;
        }

        previewTerminal = null;
        previewRoute = null;

        if (GuidedManualEnabled)
        {
            if (TryGetRouteSurface(out RaycastHit hit, out WireSurface surface) &&
                manualRouter.TryCreateAnchor(surface, hit.point, startTerminal,
                    connectionManager.Connections, manualAnchors, out ManualWireAnchor anchor))
            {
                HasValidRouteSurface = true;
                activeWire.SetRoutePreview(
                    manualRouter.BuildPreview(startTerminal, manualAnchors, anchor));
            }
            else
            {
                activeWire.SetRoutePreview(
                    manualRouter.BuildPreview(startTerminal, manualAnchors, null));
            }
            return;
        }

        Ray ray = new Ray(playerCamera.transform.position,
            playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit freeHit, routeDistance,
            wireSurfaceMask, QueryTriggerInteraction.Ignore))
        {
            HasValidRouteSurface = true;
            activeWire.UpdatePreview(freeHit.point);
            return;
        }

        Vector3 fallbackPosition = playerCamera.transform.position +
            playerCamera.transform.forward * routeDistance;
        activeWire.UpdatePreview(fallbackPosition);
    }

    private bool TryGetRouteSurface(out RaycastHit hit, out WireSurface surface)
    {
        hit = default;
        surface = null;
        if (playerCamera == null)
            return false;

        Ray ray = new Ray(playerCamera.transform.position,
            playerCamera.transform.forward);
        if (!Physics.Raycast(ray, out hit, routeDistance,
            wireSurfaceMask, QueryTriggerInteraction.Ignore))
            return false;

        surface = hit.collider.GetComponentInParent<WireSurface>();
        return surface != null && surface.isActiveAndEnabled;
    }

    private void UndoLastWaypoint()
    {
        if (!GuidedManualEnabled || manualAnchors.Count == 0)
            return;

        manualAnchors.RemoveAt(manualAnchors.Count - 1);
        activeWire.SetRoutePreview(
            manualRouter.BuildPreview(startTerminal, manualAnchors, null));
        Debug.Log($"Son kablo noktası geri alındı. Kalan: {manualAnchors.Count}");
    }

    private void CancelWire()
    {
        if (!IsWiring)
            return;
        Destroy(activeWire.gameObject);
        ResetWiringState();
        Debug.Log("Kablo işlemi iptal edildi.");
    }

    private void ResetWiringState()
    {
        activeWire = null;
        startTerminal = null;
        previewTerminal = null;
        previewRoute = null;
        manualAnchors.Clear();
        HasValidRouteSurface = false;
    }
}
