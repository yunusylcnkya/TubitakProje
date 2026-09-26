using UnityEngine;

public sealed class PlacementSlot :
    MonoBehaviour,
    IInteractable,
    IInteractionInfo
{
    [Header("Systems")]
    [SerializeField]
    private PlacementManager placementManager;

    [Header("Slot")]
    [SerializeField]
    private Transform placementPoint;

    [Header("Identity")]
    [SerializeField]
    private string slotId;

    [SerializeField]
    private string displayName = "Equipment Slot";

    [Header("Accepted Equipment")]
    [SerializeField]
    private EquipmentDefinition acceptedEquipment;

    private Equipment placedEquipment;

    public string SlotId =>
        slotId?.Trim();

    public bool IsOccupied =>
        placedEquipment != null;

    public string InteractionPrompt =>
        "Ekipmanı Yerleştir";

    // =========================================
    // ACCEPTANCE
    // =========================================

    public bool CanAccept(
        Equipment equipment)
    {
        if (equipment == null)
            return false;

        if (equipment.Definition == null)
            return false;

        if (acceptedEquipment == null)
            return false;

        return equipment.Definition ==
               acceptedEquipment;
    }

    // =========================================
    // INTERACTION
    // =========================================

    public bool CanInteract(
        InteractionContext context)
    {
        if (IsOccupied)
            return false;

        if (context.EquipmentHolder == null)
            return false;

        if (!context.EquipmentHolder.HasEquipment)
            return false;

        Equipment equipment =
            context.EquipmentHolder.CurrentEquipment;

        return CanAccept(equipment);
    }

    public void Interact(
        InteractionContext context)
    {
        if (!CanInteract(context))
            return;

        Equipment equipment =
            context.EquipmentHolder
                .ReleaseForPlacement();

        if (equipment == null)
            return;

        Place(equipment);
    }

    // =========================================
    // PLACEMENT
    // =========================================

    private void Place(
        Equipment equipment)
    {
        placedEquipment = equipment;

        Transform equipmentTransform =
            equipment.transform;

        equipmentTransform.SetParent(
            placementPoint
        );

        equipmentTransform.localPosition =
            Vector3.zero;

        equipmentTransform.localRotation =
            Quaternion.identity;

        equipment.SetPlaced();

        if (placementManager != null)
        {
            placementManager.RegisterPlacement(
                this,
                equipment
            );
        }

        Debug.Log(
            $"PLACED | " +
            $"{equipment.DisplayName} -> {SlotId}"
        );
    }

    // =========================================
    // UI
    // =========================================

    public string GetInteractionTitle(
        InteractionContext context)
    {
        return displayName;
    }

    public string GetInteractionAction(
        InteractionContext context)
    {
        if (IsOccupied)
        {
            return "Slot dolu";
        }

        if (context.EquipmentHolder == null)
        {
            return string.Empty;
        }

        if (!context.EquipmentHolder.HasEquipment)
        {
            return "Yerleştirilecek ekipman gerekli";
        }

        Equipment equipment =
            context.EquipmentHolder.CurrentEquipment;

        if (equipment == null)
        {
            return string.Empty;
        }

        if (!CanAccept(equipment))
        {
            return
                $"{equipment.DisplayName} " +
                "buraya yerleştirilemez";
        }

        return "[E] Yerleştir";
    }

    private void OnValidate()
    {
        if (!string.IsNullOrWhiteSpace(slotId))
        {
            slotId =
                slotId.Trim();
        }
    }
}