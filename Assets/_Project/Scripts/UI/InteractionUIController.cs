using UnityEngine;

public sealed class InteractionUIController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private InteractionHUD hud;

    [Header("Interaction")]
    [SerializeField] private PlayerInteractor playerInteractor;

    [Header("Wiring")]
    [SerializeField] private TerminalSelector terminalSelector;
    [SerializeField] private WireController wireController;
    [SerializeField] private WireRemovalController wireRemovalController;

    [Header("Equipment")]
    [SerializeField] private EquipmentHolder equipmentHolder;

    [Header("Hover performance - v19.20.8.1-P1")]
    [Tooltip("Interval between text/UI refreshes, in seconds. " +
             "0 = every frame. This only throttles display; input stays immediate.")]
    [SerializeField, Range(0f, 0.1f)]
    private float uiRefreshInterval = 0.03f;

    private float nextRefreshTime;
    private bool previousIsWiring;
    private bool previousHasEquipment;

    private void Update()
    {
        bool isWiring = wireController != null && wireController.IsWiring;
        bool hasEquipment = equipmentHolder != null && equipmentHolder.HasEquipment;

        // Important UI mode changes display immediately, regardless of throttle.
        bool modeChanged = isWiring != previousIsWiring ||
                           hasEquipment != previousHasEquipment;

        if (!modeChanged && Time.unscaledTime < nextRefreshTime)
            return;

        previousIsWiring = isWiring;
        previousHasEquipment = hasEquipment;
        nextRefreshTime = Time.unscaledTime + uiRefreshInterval;
        RefreshUI();
    }

    private void OnDisable()
    {
        nextRefreshTime = 0f;
        previousIsWiring = false;
        previousHasEquipment = false;
        if (hud != null)
            hud.Hide();
    }

    private void RefreshUI()
    {
        if (hud == null)
            return;

        // 1. Active wire routing
        if (wireController != null && wireController.IsWiring)
        {
            ShowWiringUI();
            return;
        }

        // 2. Equipment in hands: prioritize placement slots
        if (equipmentHolder != null && equipmentHolder.HasEquipment)
        {
            if (TryShowNormalInteraction())
                return;

            hud.Hide();
            return;
        }

        // 3. Terminal
        Terminal terminal = terminalSelector != null
            ? terminalSelector.CurrentTerminal : null;

        if (terminal != null)
        {
            ShowTerminalUI(terminal);
            return;
        }

        // 4. Completed wire
        Wire wire = wireRemovalController != null
            ? wireRemovalController.CurrentWire : null;

        if (wire != null)
        {
            ShowWireUI(wire);
            return;
        }

        // 5. Equipment / placement slot
        if (TryShowNormalInteraction())
            return;

        hud.Hide();
    }

    private bool TryShowNormalInteraction()
    {
        if (playerInteractor == null)
            return false;

        IInteractable interactable = playerInteractor.CurrentInteractable;
        if (interactable == null)
            return false;

        IInteractionInfo info = interactable as IInteractionInfo;
        if (info == null)
            return false;

        InteractionContext context = playerInteractor.Context;
        string title = info.GetInteractionTitle(context);
        string action = info.GetInteractionAction(context);
        if (string.IsNullOrWhiteSpace(title) &&
            string.IsNullOrWhiteSpace(action))
            return false;

        hud.Show(title, action);
        return true;
    }

    private void ShowWiringUI()
    {
        Terminal terminal = terminalSelector != null
            ? terminalSelector.CurrentTerminal : null;

        if (terminal != null)
        {
            ShowTerminalUI(terminal);
            return;
        }

        if (wireController.HasValidRouteSurface)
        {
            hud.Show("Kablo Güzergâhı", "[Sol Tık] Nokta Ekle");
            return;
        }

        hud.Show("Kablo Döşeme", "[Sağ Tık] İptal");
    }

    private void ShowTerminalUI(Terminal terminal)
    {
        if (wireController == null)
            return;

        if (!wireController.IsWiring)
        {
            if (!terminal.IsAvailableForWiring)
            {
                hud.Show(terminal.DisplayName, "Terminal kullanımda");
                return;
            }

            hud.Show(terminal.DisplayName, "[Sol Tık] Kablo Başlat");
            return;
        }

        if (terminal == wireController.StartTerminal)
        {
            hud.Show(terminal.DisplayName, "Başlangıç terminali");
            return;
        }

        if (!terminal.IsAvailableForWiring)
        {
            hud.Show(terminal.DisplayName, "Terminal kullanımda");
            return;
        }

        hud.Show(terminal.DisplayName, "[Sol Tık] Bağla");
    }

    private void ShowWireUI(Wire wire)
    {
        string wireName = "Kablo";
        if (wire.StartTerminal != null && wire.EndTerminal != null)
        {
            wireName = $"{wire.StartTerminal.DisplayName}" +
                       $" ↔ {wire.EndTerminal.DisplayName}";
        }

        hud.Show(wireName, "[R] Kabloyu Sök");
    }
}
