using UnityEngine;

public sealed class TerminalStateIndicator :
    MonoBehaviour
{
    [Header("Terminal")]
    [SerializeField]
    private Terminal terminal;

    [Header("Visual States")]
    [SerializeField]
    private GameObject availableVisual;

    [SerializeField]
    private GameObject connectedVisual;

    [SerializeField]
    private GameObject fullVisual;

    [Header("Behaviour")]
    [SerializeField]
    private bool hideUntilEquipmentPlaced =
        true;

    private int lastConnectionCount = -1;
    private EquipmentState lastOwnerState;

    private void Awake()
    {
        if (terminal == null)
        {
            terminal =
                GetComponent<Terminal>();
        }

        Refresh(true);
    }

    private void Update()
    {
        Refresh(false);
    }

    private void Refresh(
        bool force)
    {
        if (terminal == null)
        {
            SetAllInactive();
            return;
        }

        Equipment owner =
            terminal.Owner;

        EquipmentState ownerState =
            owner != null
                ? owner.CurrentState
                : EquipmentState.World;

        int connectionCount =
            terminal.ConnectionCount;

        if (!force &&
            connectionCount ==
                lastConnectionCount &&
            ownerState ==
                lastOwnerState)
        {
            return;
        }

        lastConnectionCount =
            connectionCount;

        lastOwnerState =
            ownerState;

        SetAllInactive();

        if (hideUntilEquipmentPlaced)
        {
            if (owner == null ||
                owner.CurrentState !=
                EquipmentState.Placed)
            {
                return;
            }
        }

        if (!terminal.CanAcceptMoreConnections)
        {
            Activate(
                fullVisual
            );

            return;
        }

        if (terminal.ConnectionCount > 0)
        {
            Activate(
                connectedVisual
            );

            return;
        }

        Activate(
            availableVisual
        );
    }

    private void SetAllInactive()
    {
        if (availableVisual != null)
        {
            availableVisual.SetActive(
                false
            );
        }

        if (connectedVisual != null)
        {
            connectedVisual.SetActive(
                false
            );
        }

        if (fullVisual != null)
        {
            fullVisual.SetActive(
                false
            );
        }
    }

    private void Activate(
        GameObject target)
    {
        if (target != null)
        {
            target.SetActive(
                true
            );
        }
    }
}