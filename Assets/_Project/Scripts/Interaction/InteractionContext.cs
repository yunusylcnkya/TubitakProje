using UnityEngine;

public readonly struct InteractionContext
{
    public GameObject Interactor { get; }
    public EquipmentHolder EquipmentHolder { get; }

    public InteractionContext(
        GameObject interactor,
        EquipmentHolder equipmentHolder)
    {
        Interactor = interactor;
        EquipmentHolder = equipmentHolder;
    }
}