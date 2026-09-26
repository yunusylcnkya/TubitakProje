using UnityEngine;

public sealed class PushButtonActuator :
    MonoBehaviour,
    IInteractable,
    IInteractionInfo,
    IHoldInteractable
{
    [Header("Device")]
    [SerializeField]
    private PushButtonDevice device;

    [Header("Interaction")]
    [SerializeField]
    private Collider interactionCollider;

    [SerializeField]
    private string displayName =
        "Buton";

    public string InteractionPrompt =>
        "[E] Basılı Tut";

    private void Update()
    {
        if (interactionCollider == null)
            return;

        bool shouldBeEnabled =
            device != null &&
            device.IsOperational;

        if (interactionCollider.enabled !=
            shouldBeEnabled)
        {
            interactionCollider.enabled =
                shouldBeEnabled;
        }
    }

    public bool CanInteract(
        InteractionContext context)
    {
        if (device == null ||
            !device.IsOperational)
        {
            return false;
        }

        if (context.EquipmentHolder != null &&
            context.EquipmentHolder.HasEquipment)
        {
            return false;
        }

        return true;
    }

    public void Interact(
        InteractionContext context)
    {
    }

    public void BeginHold(
        InteractionContext context)
    {
        if (!CanInteract(context))
            return;

        device.SetPressed(true);
    }

    public void EndHold(
        InteractionContext context)
    {
        if (device == null)
            return;

        device.SetPressed(false);
    }

    public string GetInteractionTitle(
        InteractionContext context)
    {
        if (device != null &&
            device.Owner != null)
        {
            return device.Owner.DisplayName;
        }

        return displayName;
    }

    public string GetInteractionAction(
        InteractionContext context)
    {
        if (context.EquipmentHolder != null &&
            context.EquipmentHolder.HasEquipment)
        {
            return
                "Önce elinizdeki ekipmanı bırakın";
        }

        if (device == null)
            return string.Empty;

        return device.IsPressed
            ? "Basılı"
            : "[E] Basılı Tut";
    }
}