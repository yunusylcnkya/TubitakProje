using UnityEngine;

public sealed class InteractionUIController :
    MonoBehaviour
{
    [Header("UI")]
    [SerializeField]
    private InteractionHUD hud;

    [Header("Interaction")]
    [SerializeField]
    private PlayerInteractor playerInteractor;

    [Header("Wiring")]
    [SerializeField]
    private TerminalSelector terminalSelector;

    [SerializeField]
    private WireController wireController;

    [SerializeField]
    private WireRemovalController
        wireRemovalController;

    [Header("Equipment")]
    [SerializeField]
    private EquipmentHolder equipmentHolder;

    private void Update()
    {
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (hud == null)
            return;

        // =========================================
        // 1. AKTİF KABLO ÇEKME
        // =========================================

        if (wireController != null &&
            wireController.IsWiring)
        {
            ShowWiringUI();
            return;
        }

        // =========================================
        // 2. ELDE EKİPMAN VARSA
        //    Önceliğimiz PlacementSlot.
        // =========================================

        if (equipmentHolder != null &&
            equipmentHolder.HasEquipment)
        {
            if (TryShowNormalInteraction())
                return;

            hud.Hide();
            return;
        }

        // =========================================
        // 3. TERMİNAL
        // =========================================

        Terminal terminal =
            terminalSelector != null
                ? terminalSelector.CurrentTerminal
                : null;

        if (terminal != null)
        {
            ShowTerminalUI(terminal);
            return;
        }

        // =========================================
        // 4. TAMAMLANMIŞ KABLO
        // =========================================

        Wire wire =
            wireRemovalController != null
                ? wireRemovalController.CurrentWire
                : null;

        if (wire != null)
        {
            ShowWireUI(wire);
            return;
        }

        // =========================================
        // 5. EQUIPMENT / SLOT
        // =========================================

        if (TryShowNormalInteraction())
            return;

        // =========================================

        hud.Hide();
    }

    private bool TryShowNormalInteraction()
    {
        if (playerInteractor == null)
            return false;

        IInteractable interactable =
            playerInteractor.CurrentInteractable;

        if (interactable == null)
            return false;

        IInteractionInfo info =
            interactable as IInteractionInfo;

        if (info == null)
            return false;

        InteractionContext context =
            playerInteractor.Context;

        string title =
            info.GetInteractionTitle(context);

        string action =
            info.GetInteractionAction(context);

        if (string.IsNullOrWhiteSpace(title) &&
            string.IsNullOrWhiteSpace(action))
        {
            return false;
        }

        hud.Show(
            title,
            action
        );

        return true;
    }

    private void ShowWiringUI()
    {
        Terminal terminal =
            terminalSelector != null
                ? terminalSelector.CurrentTerminal
                : null;

        // Terminal hedefi varsa
        if (terminal != null)
        {
            ShowTerminalUI(terminal);
            return;
        }

        // WireSurface hedefi varsa
        if (wireController.HasValidRouteSurface)
        {
            hud.Show(
                "Kablo Güzergâhı",
                "[Sol Tık] Nokta Ekle"
            );

            return;
        }

        // Hiçbir geçerli hedef yok.
        hud.Show(
            "Kablo Döşeme",
            "[Sağ Tık] İptal"
        );
    }

    private void ShowTerminalUI(
        Terminal terminal)
    {
        if (wireController == null)
            return;

        // -------------------------------
        // Kablo çekilmiyorsa
        // -------------------------------

        if (!wireController.IsWiring)
        {
            if (!terminal.IsAvailableForWiring)
            {
                hud.Show(
                    terminal.DisplayName,
                    "Terminal kullanımda"
                );

                return;
            }

            hud.Show(
                terminal.DisplayName,
                "[Sol Tık] Kablo Başlat"
            );

            return;
        }

        // -------------------------------
        // Kablo zaten çekiliyorsa
        // -------------------------------

        if (terminal ==
            wireController.StartTerminal)
        {
            hud.Show(
                terminal.DisplayName,
                "Başlangıç terminali"
            );

            return;
        }

        if (!terminal.IsAvailableForWiring)
        {
            hud.Show(
                terminal.DisplayName,
                "Terminal kullanımda"
            );

            return;
        }

        hud.Show(
            terminal.DisplayName,
            "[Sol Tık] Bağla"
        );
    }

    private void ShowWireUI(
        Wire wire)
    {
        string wireName =
            "Kablo";

        if (wire.StartTerminal != null &&
            wire.EndTerminal != null)
        {
            wireName =
                $"{wire.StartTerminal.DisplayName}" +
                $" ↔ " +
                $"{wire.EndTerminal.DisplayName}";
        }

        hud.Show(
            wireName,
            "[R] Kabloyu Sök"
        );
    }
}