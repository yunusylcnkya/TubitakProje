using UnityEngine;

public sealed class PlayerInteractor :
    MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Camera playerCamera;

    [SerializeField]
    private EquipmentHolder equipmentHolder;

    [Header("Interaction Settings")]
    [SerializeField, Min(0.1f)]
    private float interactionDistance = 3f;

    [SerializeField]
    private LayerMask interactionMask;

    private IInteractable currentInteractable;

    private IHoldInteractable
        activeHoldInteractable;

    public IInteractable CurrentInteractable =>
        currentInteractable;

    public InteractionContext Context =>
        new InteractionContext(
            gameObject,
            equipmentHolder
        );

    private void Awake()
    {
        if (equipmentHolder == null)
        {
            equipmentHolder =
                GetComponentInChildren<
                    EquipmentHolder>();
        }

        if (playerCamera == null)
        {
            playerCamera =
                GetComponentInChildren<Camera>();
        }
    }

    private void Update()
    {
        DetectInteractable();
        HandleInput();
    }

    private void OnDisable()
    {
        EndHoldInteraction();
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            BeginInteraction();
        }

        if (Input.GetKeyUp(KeyCode.E))
        {
            EndHoldInteraction();
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            equipmentHolder?.DropCurrent();
        }
    }

    private void BeginInteraction()
    {
        if (currentInteractable == null)
            return;

        InteractionContext context =
            Context;

        if (!currentInteractable
                .CanInteract(context))
        {
            return;
        }

        if (currentInteractable
            is IHoldInteractable holdInteractable)
        {
            activeHoldInteractable =
                holdInteractable;

            activeHoldInteractable.BeginHold(
                context
            );

            return;
        }

        currentInteractable.Interact(
            context
        );
    }

    private void EndHoldInteraction()
    {
        if (activeHoldInteractable == null)
            return;

        activeHoldInteractable.EndHold(
            Context
        );

        activeHoldInteractable = null;
    }

    private void DetectInteractable()
    {
        currentInteractable = null;

        if (playerCamera == null)
            return;

        Ray ray =
            new Ray(
                playerCamera.transform.position,
                playerCamera.transform.forward
            );

        if (!Physics.Raycast(
                ray,
                out RaycastHit hit,
                interactionDistance,
                interactionMask,
                QueryTriggerInteraction.Ignore))
        {
            return;
        }

        currentInteractable =
            hit.collider
                .GetComponentInParent<
                    IInteractable>();
    }
}