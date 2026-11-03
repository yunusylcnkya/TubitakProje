
using UnityEngine;

public sealed class PlayerInteractor : MonoBehaviour
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
    private IHoldInteractable activeHoldInteractable;

    // Son algılanan Collider
    private Collider lastDetectedCollider;

    // Tekrar kullanılacak etkileşim bağlamı
    private InteractionContext cachedContext;

    public IInteractable CurrentInteractable =>
        currentInteractable;

    public InteractionContext Context =>
        cachedContext;

    private void Awake()
    {
        if (equipmentHolder == null)
        {
            equipmentHolder =
                GetComponentInChildren<EquipmentHolder>();
        }

        if (playerCamera == null)
        {
            playerCamera =
                GetComponentInChildren<Camera>();
        }

        cachedContext = new InteractionContext(
            gameObject,
            equipmentHolder
        );
    }

    private void Update()
    {
        DetectInteractable();
        HandleInput();
    }

    private void OnDisable()
    {
        EndHoldInteraction();
        ClearDetection();
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

        InteractionContext context = Context;

        if (!currentInteractable.CanInteract(context))
            return;

        if (currentInteractable is IHoldInteractable holdInteractable)
        {
            activeHoldInteractable = holdInteractable;

            activeHoldInteractable.BeginHold(context);
            return;
        }

        currentInteractable.Interact(context);
    }

    private void EndHoldInteraction()
    {
        if (activeHoldInteractable == null)
            return;

        activeHoldInteractable.EndHold(Context);
        activeHoldInteractable = null;
    }

    private void DetectInteractable()
    {
        if (playerCamera == null)
        {
            ClearDetection();
            return;
        }

        Ray ray = new Ray(
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
            ClearDetection();
            return;
        }

        // Aynı Collider'a bakıyorsak tekrar arama.
        if (hit.collider == lastDetectedCollider)
        {
            return;
        }

        lastDetectedCollider = hit.collider;

        currentInteractable =
            hit.collider.GetComponentInParent<IInteractable>();
    }

    private void ClearDetection()
    {
        lastDetectedCollider = null;
        currentInteractable = null;
    }
}
