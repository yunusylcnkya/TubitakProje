public interface IInteractable
{
    string InteractionPrompt { get; }

    bool CanInteract(InteractionContext context);

    void Interact(InteractionContext context);
}