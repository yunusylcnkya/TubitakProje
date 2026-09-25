public interface IInteractionInfo
{
    string GetInteractionTitle(
        InteractionContext context);

    string GetInteractionAction(
        InteractionContext context);
}