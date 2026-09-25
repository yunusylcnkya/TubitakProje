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

    public bool IsWiring =>
        activeWire != null;

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
        // Oyuncunun elinde ekipman varken
        // kablolama yapılmasın.
        if (equipmentHolder != null &&
            equipmentHolder.HasEquipment)
        {
            return;
        }

        // Sol Mouse
        if (Input.GetMouseButtonDown(0))
        {
            HandleLeftClick();
        }

        // Sağ Mouse
        if (Input.GetMouseButtonDown(1))
        {
            CancelWire();
        }
    }

    private void HandleLeftClick()
    {
        Terminal terminal =
            terminalSelector.CurrentTerminal;

        // --------------------------------
        // Henüz kablo başlamadıysa
        // terminalden başlat.
        // --------------------------------

        if (!IsWiring)
        {
            TryStartWire(terminal);
            return;
        }

        // --------------------------------
        // Kablo zaten başladı.
        // Terminale bakıyorsak tamamlamayı dene.
        // --------------------------------

        if (terminal != null)
        {
            TryCompleteWire(terminal);
            return;
        }

        // --------------------------------
        // Terminal değilse WireSurface
        // üzerinde yeni nokta oluşturmayı dene.
        // --------------------------------

        TryAddRoutePoint();
    }

    private void TryStartWire(Terminal terminal)
    {
        if (terminal == null)
            return;

        if (!terminal.IsAvailableForWiring)
        {
            Debug.Log(
                $"Terminal kullanılamıyor: " +
                $"{terminal.DisplayName}"
            );

            return;
        }

        if (wirePrefab == null)
        {
            Debug.LogError(
                "Wire Prefab atanmadı."
            );

            return;
        }

        if (styleLibrary == null)
        {
            Debug.LogError(
                "Wire Style Library atanmadı."
            );

            return;
        }

        WireStyle style =
            styleLibrary.GetStyle(
                terminal.LineType
            );

        if (style == null)
        {
            Debug.LogError(
                $"WireStyle bulunamadı. " +
                $"LineType: {terminal.LineType}"
            );

            return;
        }

        startTerminal = terminal;

        activeWire =
            Instantiate(wirePrefab);

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

        activeWire.AddRoutePoint(
            hit.point
        );

        Debug.Log(
            $"Kablo noktası eklendi: {surface.name}"
        );
    }

    private void TryCompleteWire(
        Terminal targetTerminal)
    {
        if (!IsWiring)
            return;

        if (targetTerminal == null)
            return;

        // Aynı terminale geri bağlanamaz.
        if (targetTerminal == startTerminal)
        {
            Debug.Log(
                "Kablo aynı terminale bağlanamaz."
            );

            return;
        }

        if (!targetTerminal.IsAvailableForWiring)
        {
            Debug.Log(
                $"Hedef terminal kullanılamıyor: " +
                $"{targetTerminal.DisplayName}"
            );

            return;
        }

        if (!activeWire.Complete(targetTerminal))
        {
            Debug.LogWarning(
                "Wire tamamlanamadı."
            );

            return;
        }

        bool startConnected =
            startTerminal.TryConnect(activeWire);

        bool endConnected =
            targetTerminal.TryConnect(activeWire);

        // İki terminalden biri bağlantıyı
        // kabul etmezse işlemi geri al.
        if (!startConnected || !endConnected)
        {
            if (startConnected)
            {
                startTerminal.Disconnect(
                    activeWire
                );
            }

            if (endConnected)
            {
                targetTerminal.Disconnect(
                    activeWire
                );
            }

            Destroy(
                activeWire.gameObject
            );

            ResetWiringState();

            Debug.LogWarning(
                "Kablo bağlantısı tamamlanamadı."
            );

            return;
        }

        if (connectionManager != null)
        {
            connectionManager.RegisterConnection(
                startTerminal,
                targetTerminal,
                activeWire
            );
        }

        Debug.Log(
            $"Bağlantı tamamlandı | " +
            $"{startTerminal.TerminalId} -> " +
            $"{targetTerminal.TerminalId}"
        );

        ResetWiringState();
    }

    private void UpdateWirePreview()
    {
        if (!IsWiring)
            return;

        // Önce terminal kontrol edilir.
        Terminal terminal =
            terminalSelector.CurrentTerminal;

        if (terminal != null)
        {
            // Kabloyu başladığımız terminale geri snap etme.
            if (terminal != startTerminal)
            {
                activeWire.UpdatePreview(
                    terminal.ConnectionPosition
                );

                return;
            }
        }

        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        // Sonra izin verilen kablo yüzeyini kontrol et.
        if (Physics.Raycast(
                ray,
                out RaycastHit hit,
                routeDistance,
                wireSurfaceMask,
                QueryTriggerInteraction.Ignore))
        {
            activeWire.UpdatePreview(
                hit.point
            );

            return;
        }

        // Hiçbir geçerli hedef yoksa
        // kameranın önünde preview göster.
        Vector3 fallbackPosition =
            playerCamera.transform.position +
            playerCamera.transform.forward *
            routeDistance;

        activeWire.UpdatePreview(
            fallbackPosition
        );
    }

    private void CancelWire()
    {
        if (!IsWiring)
            return;

        Destroy(
            activeWire.gameObject
        );

        ResetWiringState();

        Debug.Log(
            "Kablo işlemi iptal edildi."
        );
    }

    private void ResetWiringState()
    {
        activeWire = null;
        startTerminal = null;
    }
}