using UnityEngine;

public sealed class EquipmentInfoUIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInteractor playerInteractor;
    [SerializeField] private EquipmentInfoHUD equipmentInfoHUD;

    [Header("Hover performance - v19.20.8.1-P1")]
    [Tooltip("The equipment must remain under the crosshair for this many seconds " +
             "before the large information panel changes. 0 disables stabilization.")]
    [SerializeField, Range(0f, 0.2f)]
    private float hoverStabilitySeconds = 0.07f;

    private Equipment pendingEquipment;
    private Equipment shownEquipment;
    private float pendingSince;
    private bool hasPendingTarget;

    private void Update()
    {
        if (playerInteractor == null || equipmentInfoHUD == null)
            return;

        // Only changes information display, never the underlying interaction.
        Equipment equipment = playerInteractor.CurrentInteractable as Equipment;

        if (!hasPendingTarget || equipment != pendingEquipment)
        {
            pendingEquipment = equipment;
            pendingSince = Time.unscaledTime;
            hasPendingTarget = true;
        }

        if (equipment == shownEquipment)
            return;

        // Small gaps between colliders and quick passes over equipment should
        // not repeatedly rebuild text meshes / enable and disable the panel.
        if (Time.unscaledTime - pendingSince < hoverStabilitySeconds)
            return;

        shownEquipment = equipment;
        if (shownEquipment == null)
            equipmentInfoHUD.Hide();
        else
            equipmentInfoHUD.Show(shownEquipment.Definition);
    }

    private void OnDisable()
    {
        pendingEquipment = null;
        shownEquipment = null;
        hasPendingTarget = false;
        if (equipmentInfoHUD != null)
            equipmentInfoHUD.Hide();
    }
}
