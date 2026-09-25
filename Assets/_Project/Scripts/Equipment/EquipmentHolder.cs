using UnityEngine;

public sealed class EquipmentHolder : MonoBehaviour
{
    [SerializeField] private Transform holdPoint;

    private Equipment currentEquipment;

    public bool HasEquipment => currentEquipment != null;
    public Equipment CurrentEquipment => currentEquipment;

    public bool TryPickup(Equipment equipment)
    {
        if (equipment == null)
            return false;

        if (HasEquipment)
            return false;

        if (!equipment.CanBePickedUp())
            return false;

        currentEquipment = equipment;

        equipment.SetHeld();

        Transform equipmentTransform = equipment.transform;

        equipmentTransform.SetParent(holdPoint);
        equipmentTransform.localPosition = Vector3.zero;
        equipmentTransform.localRotation = Quaternion.identity;

        return true;
    }

    public void DropCurrent()
    {
        if (!HasEquipment)
            return;

        Equipment equipment = currentEquipment;

        currentEquipment = null;

        equipment.transform.SetParent(null);

        equipment.SetWorld();
    }

    public Equipment ReleaseForPlacement()
    {
        if (!HasEquipment)
            return null;

        Equipment equipment = currentEquipment;

        currentEquipment = null;

        equipment.transform.SetParent(null);

        return equipment;
    }
}