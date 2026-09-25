using UnityEngine;

public class Equipment : MonoBehaviour, IInteractable
{
    [Header("Identity")]
    [SerializeField] private string displayName = "Equipment";

    [Header("Physics")]
    [SerializeField] private Rigidbody equipmentRigidbody;

    private EquipmentState currentState = EquipmentState.World;

    public string DisplayName => displayName;
    public EquipmentState CurrentState => currentState;

    public string InteractionPrompt =>
        currentState == EquipmentState.World
            ? $"{displayName} - Al"
            : string.Empty;

    public bool CanInteract(InteractionContext context)
    {
        if (currentState != EquipmentState.World)
            return false;

        if (context.EquipmentHolder == null)
            return false;

        return !context.EquipmentHolder.HasEquipment;
    }

    public void Interact(InteractionContext context)
    {
        if (!CanInteract(context))
            return;

        context.EquipmentHolder.TryPickup(this);
    }

    public bool CanBePickedUp()
    {
        return currentState == EquipmentState.World;
    }

    public void SetHeld()
    {
        currentState = EquipmentState.Held;

        SetPhysicsState(isKinematic: true);
    }

    public void SetWorld()
    {
        currentState = EquipmentState.World;

        SetPhysicsState(isKinematic: false);
    }

    public void SetPlaced()
    {
        currentState = EquipmentState.Placed;

        SetPhysicsState(isKinematic: true);
    }

    private void SetPhysicsState(bool isKinematic)
    {
        if (equipmentRigidbody == null)
            return;

        equipmentRigidbody.isKinematic = isKinematic;
        equipmentRigidbody.useGravity = !isKinematic;

        if (!isKinematic)
        {
            equipmentRigidbody.linearVelocity = Vector3.zero;
            equipmentRigidbody.angularVelocity = Vector3.zero;
        }
    }
}