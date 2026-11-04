
using System.Collections.Generic;
using UnityEngine;

public sealed class FuseDevice :
    ElectricalDeviceBase,
    IPlacedEquipmentControl
{
    [Header("Terminals")]
    [SerializeField]
    private Terminal inputTerminal;

    [SerializeField]
    private Terminal outputTerminal;

    [Header("Protection")]
    [SerializeField]
    private bool autoTripOnDCShortCircuit = true;

    [Header("Switch State")]
    [SerializeField]
    private bool isClosed = false;

    [SerializeField]
    private bool isBlown = false;

    [Header("Lever Animation")]
    [SerializeField]
    private Transform leverPivot;

    [SerializeField]
    private Vector3 rotationAxis = Vector3.right;

    [SerializeField]
    private float openAngle = 0f;

    [SerializeField]
    private float closedAngle = -40f;

    [SerializeField, Min(1f)]
    private float rotationSpeed = 240f;

    private Quaternion leverInitialRotation;
    private string lastTripReason;

    public bool IsClosed => isClosed;

    public bool IsBlown => isBlown;

    public string LastTripReason => lastTripReason;

    public bool IsConducting =>
        IsOperational &&
        isClosed &&
        !isBlown;

    protected override void Awake()
    {
        base.Awake();

        if (isBlown)
        {
            isClosed = false;
        }

        if (leverPivot != null)
        {
            leverInitialRotation =
                leverPivot.localRotation;

            ApplyLeverRotationImmediately();
        }
    }

    private void Update()
    {
        UpdateLeverAnimation();
    }

    // =========================================
    // PLAYER INTERACTION
    // =========================================

    public bool CanUse(InteractionContext context)
    {
        return isActiveAndEnabled &&
               IsOperational;
    }

    public void Use(InteractionContext context)
    {
        if (!CanUse(context))
            return;

        // Sigorta atmışsa ilk E basışı resetler.
        if (isBlown)
        {
            ResetFuse();
            return;
        }

        // Normal durumda kolu kaldır / indir.
        isClosed = !isClosed;

        Debug.Log(
            $"FUSE | {name} | " +
            $"{(isClosed ? "CLOSED" : "OPEN")}",
            this
        );

        // Elektriksel solver yeniden hesaplanır.
        NotifyStateChanged();
    }

    public string GetActionText(
        InteractionContext context)
    {
        if (isBlown)
        {
            return "[E] Sigortayı Resetle";
        }

        if (isClosed)
        {
            return "[E] Sigortayı İndir";
        }

        return "[E] Sigortayı Kaldır";
    }

    // =========================================
    // ELECTRICAL CONNECTION
    // =========================================

    public override void CollectActiveLinks(
        List<ElectricalLink> links)
    {
        if (!IsConducting)
            return;

        ElectricalLink link =
            new ElectricalLink(
                inputTerminal,
                outputTerminal
            );

        if (link.IsValid)
        {
            links.Add(link);
        }
    }

    // =========================================
    // ELECTRICAL PROTECTION
    // =========================================

    public override bool EvaluateState(
        ElectricalSimulationSnapshot snapshot)
    {
        if (!IsConducting)
            return false;

        if (!autoTripOnDCShortCircuit)
            return false;

        if (snapshot == null)
            return false;

        bool hasPositive =
            snapshot.HasPotential(
                outputTerminal,
                ElectricalPotentialFlags.DCPositive
            );

        bool hasNegative =
            snapshot.HasPotential(
                outputTerminal,
                ElectricalPotentialFlags.DCNegative
            );

        if (!hasPositive || !hasNegative)
        {
            return false;
        }

        TripInternal("DC_SHORT_CIRCUIT");

        return true;
    }

    // =========================================
    // TRIP / RESET
    // =========================================

    [ContextMenu("Blow Fuse")]
    public void Blow()
    {
        if (isBlown)
            return;

        TripInternal("MANUAL");
    }

    private void TripInternal(string reason)
    {
        if (isBlown)
            return;

        isBlown = true;
        isClosed = false;

        lastTripReason = reason;

        Debug.LogWarning(
            $"FUSE | {name} | TRIPPED | {reason}",
            this
        );

        NotifyStateChanged();
    }

    [ContextMenu("Reset Fuse")]
    public void ResetFuse()
    {
        if (!isBlown)
            return;

        isBlown = false;
        isClosed = false;

        lastTripReason = string.Empty;

        Debug.Log(
            $"FUSE | {name} | RESET",
            this
        );

        NotifyStateChanged();
    }

    // =========================================
    // LEVER ANIMATION
    // =========================================

    private void UpdateLeverAnimation()
    {
        if (leverPivot == null)
            return;

        Quaternion targetRotation =
            GetTargetLeverRotation();

        leverPivot.localRotation =
            Quaternion.RotateTowards(
                leverPivot.localRotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
    }

    private void ApplyLeverRotationImmediately()
    {
        if (leverPivot == null)
            return;

        leverPivot.localRotation =
            GetTargetLeverRotation();
    }

    private Quaternion GetTargetLeverRotation()
    {
        float targetAngle =
            IsConducting
                ? closedAngle
                : openAngle;

        Vector3 axis =
            rotationAxis.sqrMagnitude > 0.0001f
                ? rotationAxis.normalized
                : Vector3.right;

        return leverInitialRotation *
               Quaternion.AngleAxis(
                   targetAngle,
                   axis
               );
    }
}
