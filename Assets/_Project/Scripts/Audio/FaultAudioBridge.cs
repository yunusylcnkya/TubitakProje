using System.Collections.Generic;
using UnityEngine;

public sealed class FaultAudioBridge :
    MonoBehaviour
{
    [SerializeField]
    private ElectricalFaultSystem
        faultSystem;

    private readonly HashSet<ElectricalFaultType>
        previousFaultTypes =
            new HashSet<ElectricalFaultType>();

    private void OnEnable()
    {
        CaptureCurrentFaults();

        if (faultSystem != null)
        {
            faultSystem.FaultsChanged +=
                HandleFaultsChanged;
        }
    }

    private void OnDisable()
    {
        if (faultSystem != null)
        {
            faultSystem.FaultsChanged -=
                HandleFaultsChanged;
        }
    }

    private void HandleFaultsChanged()
    {
        if (faultSystem == null)
            return;

        IReadOnlyList<ElectricalFault>
            faults =
                faultSystem.ActiveFaults;

        bool newFaultDetected =
            false;

        HashSet<ElectricalFaultType>
            currentTypes =
                new HashSet<ElectricalFaultType>();

        for (
            int i = 0;
            i < faults.Count;
            i++)
        {
            ElectricalFault fault =
                faults[i];

            if (fault == null)
                continue;

            currentTypes.Add(
                fault.Type
            );

            if (!previousFaultTypes.Contains(
                    fault.Type))
            {
                newFaultDetected =
                    true;
            }
        }

        previousFaultTypes.Clear();

        foreach (
            ElectricalFaultType type
            in currentTypes)
        {
            previousFaultTypes.Add(
                type
            );
        }

        if (newFaultDetected)
        {
            AudioManager.Instance?.Play(
                GameAudioCue.FaultWarning
            );
        }
    }

    private void CaptureCurrentFaults()
    {
        previousFaultTypes.Clear();

        if (faultSystem == null)
            return;

        IReadOnlyList<ElectricalFault>
            faults =
                faultSystem.ActiveFaults;

        for (
            int i = 0;
            i < faults.Count;
            i++)
        {
            ElectricalFault fault =
                faults[i];

            if (fault == null)
                continue;

            previousFaultTypes.Add(
                fault.Type
            );
        }
    }
}