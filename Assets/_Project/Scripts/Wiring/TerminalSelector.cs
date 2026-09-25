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

    private Terminal currentTerminal;

    public Terminal CurrentTerminal =>
        currentTerminal;

    private void Update()
    {
        DetectTerminal();
    }

    private void DetectTerminal()
    {
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
    }
}