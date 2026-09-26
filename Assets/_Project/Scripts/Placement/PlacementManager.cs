using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class PlacementManager :
    MonoBehaviour
{
    private readonly Dictionary<string, Equipment>
        placedEquipmentBySlot =
            new Dictionary<string, Equipment>();

    public event Action PlacementsChanged;

    // =========================================
    // REGISTER
    // =========================================

    public void RegisterPlacement(
        PlacementSlot slot,
        Equipment equipment)
    {
        if (slot == null ||
            equipment == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(
                slot.SlotId))
        {
            Debug.LogError(
                "PlacementSlot için SlotId tanımlanmamış.",
                slot
            );

            return;
        }

        placedEquipmentBySlot[
            slot.SlotId
        ] = equipment;

        Debug.Log(
            $"PLACEMENT REGISTERED | " +
            $"Slot: {slot.SlotId} | " +
            $"Equipment: {equipment.DisplayName}"
        );

        PlacementsChanged?.Invoke();
    }

    // =========================================
    // QUERY
    // =========================================

    public bool IsEquipmentPlaced(
        EquipmentDefinition definition,
        string slotId)
    {
        if (definition == null)
            return false;

        if (string.IsNullOrWhiteSpace(
                slotId))
        {
            return false;
        }

        string normalizedSlotId =
            slotId.Trim();

        if (!placedEquipmentBySlot.TryGetValue(
                normalizedSlotId,
                out Equipment equipment))
        {
            return false;
        }

        if (equipment == null)
            return false;

        return equipment.Definition ==
               definition;
    }

    // =========================================
    // REMOVE
    // =========================================

    public void UnregisterPlacement(
        PlacementSlot slot)
    {
        if (slot == null)
            return;

        if (!placedEquipmentBySlot.Remove(
                slot.SlotId))
        {
            return;
        }

        PlacementsChanged?.Invoke();
    }
}