using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class ElectricalDeviceBase :
    MonoBehaviour
{
    [Header("Owner")]
    [SerializeField]
    private Equipment owner;

    public Equipment Owner =>
        owner;

    public bool IsOperational =>
        owner != null &&
        owner.CurrentState ==
            EquipmentState.Placed;

    public event Action StateChanged;

    protected virtual void Awake()
    {
        if (owner == null)
        {
            owner =
                GetComponent<Equipment>();
        }

        if (owner == null)
        {
            Debug.LogError(
                $"{name}: ElectricalDeviceBase " +
                "Equipment owner bulamadı.",
                this
            );
        }
    }

    protected void NotifyStateChanged()
    {
        StateChanged?.Invoke();
    }

    public abstract void CollectActiveLinks(
        List<ElectricalLink> links
    );

    public virtual bool EvaluateState(
        ElectricalSimulationSnapshot snapshot)
    {
        return false;
    }
}