using UnityEngine;

public sealed class TerminalSelector : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Camera playerCamera;

    [Header("Detection")]
    [SerializeField, Min(0.1f)]
    private float detectionDistance = 5f;

    [SerializeField]
    private LayerMask terminalMask;


    private TerminalHighlight currentHighlight;

    private Terminal currentTerminal;


    public Terminal CurrentTerminal =>
        currentTerminal;

    private void Update()
    {
        DetectTerminal();
    }

    private void DetectTerminal()
    {
        if (currentHighlight != null)
        {
            currentHighlight.SetHighlighted(false);
            currentHighlight = null;
        }

        currentTerminal = null;


        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        if (!Physics.Raycast(
                ray,
                out RaycastHit hit,
                detectionDistance,
                terminalMask,
                QueryTriggerInteraction.Ignore))
        {
            return;
        }

        currentTerminal =
    hit.collider.GetComponentInParent<Terminal>();

        if (currentTerminal != null)
        {
            currentHighlight =
                currentTerminal.GetComponent<TerminalHighlight>();

            if (currentHighlight != null)
            {
                currentHighlight.SetHighlighted(true);
            }
        }
    }
}