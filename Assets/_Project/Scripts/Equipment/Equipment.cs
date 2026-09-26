using UnityEngine;

public class Equipment :
    MonoBehaviour,
    IInteractable,
    IInteractionInfo
{
    [Header("Definition")]
    [SerializeField]
    private EquipmentDefinition definition;

    [Header("Physics")]
    [SerializeField]
    private Rigidbody equipmentRigidbody;

    private EquipmentState currentState =
        EquipmentState.World;

    // =========================================
    // PUBLIC DATA
    // =========================================

    public EquipmentDefinition Definition =>
        definition;

    public string EquipmentId =>
        definition != null
            ? definition.EquipmentId
            : string.Empty;

    public string DisplayName =>
        definition != null
            ? definition.DisplayName
            : gameObject.name;

    public EquipmentState CurrentState =>
        currentState;

    // IInteractable'ın mevcut sözleşmesi
    public string InteractionPrompt =>
        currentState == EquipmentState.World
            ? $"{DisplayName} - Al"
            : string.Empty;

    // =========================================
    // INTERACTION
    // =========================================

    public bool CanInteract(
        InteractionContext context)
    {
        if (currentState != EquipmentState.World)
            return false;

        if (context.EquipmentHolder == null)
            return false;

        return !context.EquipmentHolder.HasEquipment;
    }

    public void Interact(
        InteractionContext context)
    {
        if (!CanInteract(context))
            return;

        context.EquipmentHolder.TryPickup(this);
    }

    // =========================================
    // EQUIPMENT STATE
    // =========================================

    public bool CanBePickedUp()
    {
        return currentState ==
               EquipmentState.World;
    }

    public void SetHeld()
    {
        currentState =
            EquipmentState.Held;

        SetPhysicsState(true);
    }

    public void SetWorld()
    {
        currentState =
            EquipmentState.World;

        SetPhysicsState(false);
    }

    public void SetPlaced()
    {
        currentState =
            EquipmentState.Placed;

        SetPhysicsState(true);
    }

    // =========================================
    // PHYSICS
    // =========================================

    private void SetPhysicsState(
        bool isKinematic)
    {
        if (equipmentRigidbody == null)
            return;

        equipmentRigidbody.isKinematic =
            isKinematic;

        equipmentRigidbody.useGravity =
            !isKinematic;

        if (!isKinematic)
        {
            equipmentRigidbody.linearVelocity =
                Vector3.zero;

            equipmentRigidbody.angularVelocity =
                Vector3.zero;
        }
    }

    // =========================================
    // UI
    // =========================================

    public string GetInteractionTitle(
        InteractionContext context)
    {
        return DisplayName;
    }

    public string GetInteractionAction(
        InteractionContext context)
    {
        switch (currentState)
        {
            case EquipmentState.World:

                if (context.EquipmentHolder != null &&
                    context.EquipmentHolder.HasEquipment)
                {
                    return
                        "Önce elinizdeki ekipmanı bırakın";
                }

                return "[E] Al";

            case EquipmentState.Held:
                return "Elde";

            case EquipmentState.Placed:
                return "Yerleştirildi";

            default:
                return string.Empty;
        }
    }
}