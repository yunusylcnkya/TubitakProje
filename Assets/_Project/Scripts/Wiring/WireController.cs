
using UnityEngine;

public sealed class WireController : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Camera playerCamera;

    [SerializeField]
    private TerminalSelector terminalSelector;

    [SerializeField]
    private EquipmentHolder equipmentHolder;

    [SerializeField]
    private ConnectionManager connectionManager;

    [SerializeField]
    private ConnectionFeedbackUI feedbackUI;

    [Header("Wire")]
    [SerializeField]
    private Wire wirePrefab;

    [SerializeField]
    private WireStyleLibrary styleLibrary;

    [Header("Route Settings")]
    [SerializeField, Min(0.1f)]
    private float routeDistance = 5f;

    [SerializeField]
    private LayerMask wireSurfaceMask;

    private Terminal startTerminal;
    private Wire activeWire;

    public Terminal StartTerminal => startTerminal;
    public bool IsWiring => activeWire != null;

    public bool HasValidRouteSurface
    {
        get;
        private set;
    }

    private void Update()
    {
        if (IsWiring)
        {
            UpdateWirePreview();
        }

        HandleInput();
    }

    private void HandleInput()
    {
        if (equipmentHolder != null &&
            equipmentHolder.HasEquipment)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            HandleLeftClick();
        }

        if (Input.GetMouseButtonDown(1))
        {
            CancelWire();
        }
    }

    private void HandleLeftClick()
    {
        Terminal terminal =
            terminalSelector.CurrentTerminal;

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

    // =========================================
    // START WIRE
    // =========================================

    private void TryStartWire(Terminal terminal)
    {
        if (terminal == null)
            return;

        if (!terminal.IsAvailableForWiring)
        {
            Debug.Log(
                $"Terminal kullanılamıyor: {terminal.DisplayName}"
            );
            return;
        }

        if (wirePrefab == null)
        {
            Debug.LogError("Wire Prefab atanmadı.");
            return;
        }

        if (styleLibrary == null)
        {
            Debug.LogError("Wire Style Library atanmadı.");
            return;
        }

        if (connectionManager == null)
        {
            Debug.LogError("ConnectionManager atanmadı.");
            return;
        }

        WireStyle style =
            styleLibrary.GetStyle(terminal.LineType);

        if (style == null)
        {
            Debug.LogError(
                $"WireStyle bulunamadı: {terminal.LineType}"
            );
            return;
        }

        startTerminal = terminal;

        activeWire = Instantiate(wirePrefab);

        activeWire.Begin(
            startTerminal,
            style
        );

        Debug.Log(
            $"Kablo başladı | " +
            $"Terminal: {startTerminal.TerminalId} | " +
            $"Hat: {startTerminal.LineType}"
        );
    }

    // =========================================
    // ADD ROUTE POINT
    // =========================================

    private void TryAddRoutePoint()
    {
        if (!IsWiring)
            return;

        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        if (!Physics.Raycast(
            ray,
            out RaycastHit hit,
            routeDistance,
            wireSurfaceMask,
            QueryTriggerInteraction.Ignore))
        {
            Debug.Log(
                "Bu yüzeye kablo noktası eklenemez."
            );
            return;
        }

        WireSurface surface =
            hit.collider.GetComponentInParent<WireSurface>();

        if (surface == null)
        {
            Debug.Log(
                "Geçerli WireSurface bulunamadı."
            );
            return;
        }

        activeWire.AddRoutePoint(hit.point);

        Debug.Log(
            $"Kablo noktası eklendi: {surface.name}"
        );
    }

    // =========================================
    // COMPLETE WIRE
    // =========================================

    private void TryCompleteWire(
        Terminal targetTerminal)
    {
        if (!IsWiring || targetTerminal == null)
            return;

        if (targetTerminal == startTerminal)
        {
            Debug.Log(
                "Kablo aynı terminale bağlanamaz."
            );
            return;
        }


        if (!targetTerminal.IsAvailableForWiring)
        {
            ConnectionValidationResult result =
                new ConnectionValidationResult(
                    ConnectionIssueSeverity.Warning,
                    $"'{targetTerminal.DisplayName}' terminali " +
                    "kullanılamıyor. Bağlantı kapasitesini " +
                    "ve ekipmanın yerleşim durumunu kontrol edin.",
                    false
                );

            ShowFeedback(result);
            return;
        }


        // 1. Electrical validation.
        ConnectionValidationResult validation =
            connectionManager.ValidateConnection(
                startTerminal,
                targetTerminal
            );
        Debug.Log(
            $"[WIRE VALIDATION] " +
            $"Start: {startTerminal.LineType} | " +
            $"Target: {targetTerminal.LineType} | " +
            $"Severity: {validation.Severity} | " +
            $"Allowed: {validation.IsAllowed} | " +
            $"Message: {validation.Message}",
            this
        );
        if (!validation.IsAllowed)
        {
            ShowFeedback(validation);

            Debug.LogWarning(
                $"Bağlantı engellendi: {validation.Message}"
            );

            // Kablo preview olarak kalır.
            // Oyuncu farklı terminal seçebilir.
            return;
        }

        // 2. Remember previous short-circuit state.
        ElectricalSimulationSystem simulation =
            connectionManager.ElectricalSimulation;

        bool hadShortCircuit =
            simulation != null &&
            simulation.LastSnapshot != null &&
            simulation.LastSnapshot.HasShortCircuit;

        // 3. Complete wire geometry.
        if (!activeWire.Complete(targetTerminal))
        {
            Debug.LogWarning(
                "Wire tamamlanamadı."
            );
            return;
        }

        // 4. Connect terminal references.
        bool startConnected =
            startTerminal.TryConnect(activeWire);

        bool endConnected =
            targetTerminal.TryConnect(activeWire);

        if (!startConnected || !endConnected)
        {
            RollbackConnection(
                targetTerminal,
                startConnected,
                endConnected
            );

            Debug.LogWarning(
                "Terminal bağlantısı tamamlanamadı."
            );

            return;
        }

        // 5. Register in connection manager.
        bool registered =
            connectionManager.RegisterConnection(
                startTerminal,
                targetTerminal,
                activeWire
            );

        if (!registered)
        {
            RollbackConnection(
                targetTerminal,
                true,
                true
            );

            Debug.LogWarning(
                "ConnectionManager bağlantıyı kaydetmedi."
            );

            return;
        }

        Debug.Log(
            $"Bağlantı tamamlandı | " +
            $"{startTerminal.TerminalId} -> " +
            $"{targetTerminal.TerminalId}"
        );

        // 6. Inspect resulting simulation state.
        bool newShortCircuit =
            simulation != null &&
            simulation.LastSnapshot != null &&
            simulation.LastSnapshot.HasShortCircuit;

        if (!hadShortCircuit && newShortCircuit)
        {
            string message =
                validation.Severity == ConnectionIssueSeverity.Danger
                    ? validation.Message +
                      " Simülasyon kısa devre algıladı."
                    : "Yeni bağlantı sonrasında " +
                      "simülasyon kısa devre algıladı.";

            ShowFeedback(
                new ConnectionValidationResult(
                    ConnectionIssueSeverity.Danger,
                    message,
                    true
                )
            );
        }
        else
        {
            ShowFeedback(validation);
        }

        ResetWiringState();
    }

    // =========================================
    // ROLLBACK
    // =========================================

    private void RollbackConnection(
        Terminal targetTerminal,
        bool startConnected,
        bool endConnected)
    {
        if (activeWire != null)
        {
            if (startConnected && startTerminal != null)
            {
                startTerminal.Disconnect(activeWire);
            }

            if (endConnected && targetTerminal != null)
            {
                targetTerminal.Disconnect(activeWire);
            }

            Destroy(activeWire.gameObject);
        }

        ResetWiringState();
    }

    // =========================================
    // FEEDBACK
    // =========================================

    private void ShowFeedback(
        ConnectionValidationResult result)
    {
        if (result.Severity == ConnectionIssueSeverity.None)
            return;

        if (feedbackUI != null)
        {
            feedbackUI.Show(result);
        }
        else
        {
            Debug.LogWarning(result.Message);
        }
    }

    // =========================================
    // PREVIEW
    // =========================================

    private void UpdateWirePreview()
    {
        if (!IsWiring)
            return;

        HasValidRouteSurface = false;

        Terminal terminal =
            terminalSelector.CurrentTerminal;

        if (terminal != null &&
            terminal != startTerminal)
        {
            activeWire.UpdatePreview(
                terminal.ConnectionPosition
            );

            return;
        }

        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        if (Physics.Raycast(
            ray,
            out RaycastHit hit,
            routeDistance,
            wireSurfaceMask,
            QueryTriggerInteraction.Ignore))
        {
            HasValidRouteSurface = true;

            activeWire.UpdatePreview(hit.point);
            return;
        }

        Vector3 fallbackPosition =
            playerCamera.transform.position +
            playerCamera.transform.forward *
            routeDistance;

        activeWire.UpdatePreview(fallbackPosition);
    }

    // =========================================
    // CANCEL
    // =========================================

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
        HasValidRouteSurface = false;
    }
}
