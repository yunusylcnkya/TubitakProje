using UnityEngine;

[RequireComponent(
    typeof(Equipment)
)]
public sealed class EquipmentStateAudioFeedback :
    MonoBehaviour
{
    private Equipment equipment;

    private EquipmentState previousState;

    private bool initialized;

    private void Awake()
    {
        equipment =
            GetComponent<Equipment>();

        if (equipment == null)
            return;

        previousState =
            equipment.CurrentState;

        initialized = true;
    }

    private void Update()
    {
        if (!initialized ||
            equipment == null)
        {
            return;
        }

        EquipmentState currentState =
            equipment.CurrentState;

        if (currentState ==
            previousState)
        {
            return;
        }

        HandleStateChange(
            previousState,
            currentState
        );

        previousState =
            currentState;
    }

    private void HandleStateChange(
        EquipmentState oldState,
        EquipmentState newState)
    {
        if (oldState == EquipmentState.World &&
            newState == EquipmentState.Held)
        {
            AudioManager.Instance?.Play(
                GameAudioCue.EquipmentPickup
            );

            return;
        }

        if (oldState == EquipmentState.Held &&
            newState == EquipmentState.World)
        {
            AudioManager.Instance?.Play(
                GameAudioCue.EquipmentDrop
            );

            return;
        }

        if (oldState == EquipmentState.Held &&
            newState == EquipmentState.Placed)
        {
            AudioManager.Instance?.Play(
                GameAudioCue.EquipmentPlaced
            );
        }
    }
}