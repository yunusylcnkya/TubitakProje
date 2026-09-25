using UnityEngine;

public sealed class PlacementSlot : MonoBehaviour, IInteractable
{
    [Header("Slot")]
    [SerializeField] private Transform placementPoint;

    [Header("Accepted Equipment")]
    [SerializeField] private string acceptedEquipmentName;

    private Equipment placedEquipment;

    public bool IsOccupied => placedEquipment != null;

    public string InteractionPrompt => "Ekipmanı Yerleştir";

    public bool CanInteract(InteractionContext context)
    {
        if (IsOccupied)
            return false;

        if (context.EquipmentHolder == null)
            return false;

        if (!context.EquipmentHolder.HasEquipment)
            return false;

        return CanAccept(
            context.EquipmentHolder.CurrentEquipment
        );
    }

    public void Interact(InteractionContext context)
    {
        if (!CanInteract(context))
            return;

        Equipment equipment =
            context.EquipmentHolder.ReleaseForPlacement();

        if (equipment == null)
            return;

        Place(equipment);
    }

    public bool CanAccept(Equipment equipment)
    {
        if (equipment == null)
            return false;

        return equipment.DisplayName ==
               acceptedEquipmentName;
    }

    private void Place(Equipment equipment)
    {
        placedEquipment = equipment;

        equipment.transform.SetParent(placementPoint);

        equipment.transform.localPosition =
            Vector3.zero;

        equipment.transform.localRotation =
            Quaternion.identity;

        equipment.SetPlaced();

        Debug.Log(
            $"{equipment.DisplayName} slota yerleştirildi."
        );
    }
}