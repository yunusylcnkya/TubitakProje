using UnityEngine;

public sealed class EquipmentInfoUIController :
    MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private PlayerInteractor playerInteractor;

    [SerializeField]
    private EquipmentInfoHUD equipmentInfoHUD;

    private Equipment lastEquipment;

    private void Update()
    {
        Refresh();
    }

    private void Refresh()
    {
        if (playerInteractor == null ||
            equipmentInfoHUD == null)
        {
            return;
        }

        Equipment equipment =
            playerInteractor.CurrentInteractable
                as Equipment;

        if (equipment == null)
        {
            if (lastEquipment != null)
            {
                lastEquipment = null;

                equipmentInfoHUD.Hide();
            }

            return;
        }

        if (equipment == lastEquipment)
            return;

        lastEquipment = equipment;

        equipmentInfoHUD.Show(
            equipment.Definition
        );
    }
}