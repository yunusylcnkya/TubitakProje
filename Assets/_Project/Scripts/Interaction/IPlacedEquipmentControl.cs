
public interface IPlacedEquipmentControl
{
    bool CanUse(InteractionContext context);

    void Use(InteractionContext context);

    string GetActionText(InteractionContext context);
}
