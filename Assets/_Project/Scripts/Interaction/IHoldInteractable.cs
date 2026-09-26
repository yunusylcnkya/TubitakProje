public interface IHoldInteractable
{
    void BeginHold(
        InteractionContext context);

    void EndHold(
        InteractionContext context);
}