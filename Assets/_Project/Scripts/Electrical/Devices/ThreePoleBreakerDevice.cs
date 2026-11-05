
using System.Collections.Generic;
using UnityEngine;

public sealed class ThreePoleBreakerDevice :
    ElectricalDeviceBase,
    IPlacedEquipmentControl
{
    [Header("Phase L1")]
    [SerializeField] private Terminal l1Input;
    [SerializeField] private Terminal l1Output;

    [Header("Phase L2")]
    [SerializeField] private Terminal l2Input;
    [SerializeField] private Terminal l2Output;

    [Header("Phase L3")]
    [SerializeField] private Terminal l3Input;
    [SerializeField] private Terminal l3Output;

    [Header("Switch State")]
    [SerializeField] private bool isClosed;
    [SerializeField] private bool isTripped;

    [Header("Lever Animation")]
    [SerializeField] private Transform leverPivot;
    [SerializeField] private Vector3 rotationAxis = Vector3.right;
    [SerializeField] private float openAngle = 0f;
    [SerializeField] private float closedAngle = -40f;
    [SerializeField, Min(1f)]
    private float rotationSpeed = 240f;

    private Quaternion leverInitialRotation;

    public bool IsClosed => isClosed;
    public bool IsTripped => isTripped;

    public bool IsConducting =>
        IsOperational && isClosed && !isTripped;

    protected override void Awake()
    {
        base.Awake();

        if (isTripped)
            isClosed = false;

        if (leverPivot != null)
        {
            leverInitialRotation = leverPivot.localRotation;
            ApplyLeverImmediately();
        }
    }

    private void Update()
    {
        if (leverPivot == null)
            return;

        Quaternion target = GetTargetRotation();

        leverPivot.localRotation = Quaternion.RotateTowards(
            leverPivot.localRotation,
            target,
            rotationSpeed * Time.deltaTime
        );
    }

    public bool CanUse(InteractionContext context)
    {
        return isActiveAndEnabled && IsOperational;
    }

    public void Use(InteractionContext context)
    {
        if (!CanUse(context))
            return;

        if (isTripped)
        {
            ResetBreaker();
            return;
        }

        isClosed = !isClosed;

        Debug.Log(
            $"3P BREAKER | {name} | " +
            (isClosed ? "CLOSED" : "OPEN"),
            this
        );

        NotifyStateChanged();
    }

    public string GetActionText(InteractionContext context)
    {
        if (isTripped)
            return "[E] 3P Sigortayı Resetle";

        return isClosed
            ? "[E] 3P Sigortayı İndir"
            : "[E] 3P Sigortayı Kaldır";
    }

    public override void CollectActiveLinks(
        List<ElectricalLink> links)
    {
        if (!IsConducting || links == null)
            return;

        AddLink(links, l1Input, l1Output);
        AddLink(links, l2Input, l2Output);
        AddLink(links, l3Input, l3Output);
    }

    private void AddLink(
        List<ElectricalLink> links,
        Terminal input,
        Terminal output)
    {
        ElectricalLink link =
            new ElectricalLink(input, output);

        if (link.IsValid)
            links.Add(link);
    }

    [ContextMenu("Trip 3P Breaker")]
    public void Trip()
    {
        if (isTripped)
            return;

        isTripped = true;
        isClosed = false;

        Debug.LogWarning(
            $"3P BREAKER | {name} | TRIPPED",
            this
        );

        NotifyStateChanged();
    }

    [ContextMenu("Reset 3P Breaker")]
    public void ResetBreaker()
    {
        if (!isTripped)
            return;

        isTripped = false;
        isClosed = false;

        Debug.Log(
            $"3P BREAKER | {name} | RESET",
            this
        );

        NotifyStateChanged();
    }

    private void ApplyLeverImmediately()
    {
        if (leverPivot != null)
            leverPivot.localRotation = GetTargetRotation();
    }

    private Quaternion GetTargetRotation()
    {
        Vector3 axis = rotationAxis.sqrMagnitude > 0.0001f
            ? rotationAxis.normalized
            : Vector3.right;

        float angle = isClosed && !isTripped
            ? closedAngle
            : openAngle;

        return leverInitialRotation *
               Quaternion.AngleAxis(angle, axis);
    }

    private void OnValidate()
    {
        rotationSpeed = Mathf.Max(1f, rotationSpeed);
    }
}
