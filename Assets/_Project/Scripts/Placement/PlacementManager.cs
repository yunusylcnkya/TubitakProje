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

    public void RegisterPlacement(
     PlacementSlot slot,
     Equipment equipment)
    {
        if (slot == null ||
            equipment == null)
        {
            Debug.LogError(
                "RegisterPlacement: Slot veya Equipment NULL"
            );

            return;
        }

        if (string.IsNullOrWhiteSpace(slot.SlotId))
        {
            Debug.LogError(
                $"SlotId tanımlanmamış: {slot.name}",
                slot
            );

            return;
        }

        string slotId =
            slot.SlotId.Trim();

        string equipmentId =
            equipment.EquipmentId?.Trim();

        placedEquipmentBySlot[slotId] =
            equipment;

        Debug.Log(
            $"PLACEMENT REGISTERED | " +
            $"Slot: '{slotId}' | " +
            $"Equipment: '{equipmentId}'"
        );

        PlacementsChanged?.Invoke();
    }

    public bool IsEquipmentPlaced(
     string equipmentId,
     string slotId)
    {
        if (string.IsNullOrWhiteSpace(
                equipmentId) ||
            string.IsNullOrWhiteSpace(
                slotId))
        {
            return false;
        }

        string normalizedSlotId =
            slotId.Trim();

        string normalizedEquipmentId =
            equipmentId.Trim();

        if (!placedEquipmentBySlot.TryGetValue(
                normalizedSlotId,
                out Equipment equipment))
        {
            Debug.Log(
                $"PLACEMENT CHECK FALSE | " +
                $"Slot kayıtlı değil: '{normalizedSlotId}'"
            );

            return false;
        }

        if (equipment == null)
            return false;

        bool result =
            string.Equals(
                equipment.EquipmentId?.Trim(),
                normalizedEquipmentId,
                System.StringComparison.Ordinal
            );

        Debug.Log(
            $"PLACEMENT CHECK | " +
            $"Beklenen Equipment: '{normalizedEquipmentId}' | " +
            $"Bulunan Equipment: '{equipment.EquipmentId?.Trim()}' | " +
            $"Slot: '{normalizedSlotId}' | " +
            $"Result: {result}"
        );

        return result;
    }
}