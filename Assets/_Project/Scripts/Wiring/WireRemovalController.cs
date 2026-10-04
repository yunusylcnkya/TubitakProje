using UnityEngine;

public sealed class WireRemovalController : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Camera playerCamera;

    [SerializeField]
    private ConnectionManager connectionManager;

    [SerializeField]
    private EquipmentHolder equipmentHolder;

    [SerializeField]
    private ElectricalSafetySystem safetySystem;

    [Header("Detection")]
    [SerializeField, Min(0.1f)]
    private float detectionDistance = 5f;

    [SerializeField]
    private LayerMask wireMask;

    private Wire currentWire;

    public Wire CurrentWire =>
        currentWire;

    private void Update()
    {
        DetectWire();
        HandleInput();
    }

    private void DetectWire()
    {
        currentWire = null;

        if (playerCamera == null)
            return;

        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        if (!Physics.Raycast(
                ray,
                out RaycastHit hit,
                detectionDistance,
                wireMask,
                QueryTriggerInteraction.Ignore))
        {
            return;
        }

        WireSegment segment =
            hit.collider.GetComponent<WireSegment>();

        if (segment == null)
            return;

        currentWire =
            segment.Owner;
    }

    private void HandleInput()
    {
        if (!Input.GetKeyDown(KeyCode.R))
            return;

        if (currentWire == null)
            return;

        if (equipmentHolder != null &&
            equipmentHolder.HasEquipment)
        {
            return;
        }

        RemoveCurrentWire();
    }

    private void RemoveCurrentWire()
    {
        if (currentWire == null)
            return;

        Wire wireToRemove =
            currentWire;

        // Önce güvenlik kontrolü
        if (safetySystem != null)
        {
            bool authorized =
                safetySystem.AuthorizeWireRemoval(
                    wireToRemove
                );

            if (!authorized)
                return;
        }

        Terminal start =
            wireToRemove.StartTerminal;

        Terminal end =
            wireToRemove.EndTerminal;

        if (start != null)
        {
            start.Disconnect(
                wireToRemove
            );
        }

        if (end != null)
        {
            end.Disconnect(
                wireToRemove
            );
        }

        if (connectionManager != null)
        {
            connectionManager.RemoveConnection(
                wireToRemove
            );
        }

        currentWire = null;

        Destroy(
            wireToRemove.gameObject
        );

        Debug.Log(
            "Kablo söküldü."
        );
    }
}