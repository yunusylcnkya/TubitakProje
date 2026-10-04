using System.Collections;
using UnityEngine;

public sealed class FaultUIController :
    MonoBehaviour
{
    [Header("Systems")]
    [SerializeField]
    private ElectricalFaultSystem
        faultSystem;

    [SerializeField]
    private ElectricalSafetySystem
        safetySystem;

    [Header("HUD")]
    [SerializeField]
    private FaultHUD faultHUD;

    [Header("Safety Message")]
    [SerializeField, Min(0.1f)]
    private float safetyMessageDuration = 4f;

    private Coroutine
        safetyMessageCoroutine;

    private void OnEnable()
    {
        if (faultSystem != null)
        {
            faultSystem.FaultsChanged +=
                RefreshFaults;
        }

        if (safetySystem != null)
        {
            safetySystem
                .SafetyViolationRaised +=
                HandleSafetyViolation;
        }
    }

    private void Start()
    {
        RefreshFaults();
    }

    private void OnDisable()
    {
        if (faultSystem != null)
        {
            faultSystem.FaultsChanged -=
                RefreshFaults;
        }

        if (safetySystem != null)
        {
            safetySystem
                .SafetyViolationRaised -=
                HandleSafetyViolation;
        }

        if (safetyMessageCoroutine != null)
        {
            StopCoroutine(
                safetyMessageCoroutine
            );

            safetyMessageCoroutine = null;
        }
    }

    private void RefreshFaults()
    {
        if (faultSystem == null ||
            faultHUD == null)
        {
            return;
        }

        faultHUD.SetFaults(
            faultSystem.ActiveFaults
        );
    }

    private void HandleSafetyViolation(
        string message)
    {
        if (faultHUD == null)
            return;

        faultHUD.SetSafetyMessage(
            message
        );

        if (safetyMessageCoroutine != null)
        {
            StopCoroutine(
                safetyMessageCoroutine
            );
        }

        safetyMessageCoroutine =
            StartCoroutine(
                ClearSafetyMessageAfterDelay()
            );
    }

    private IEnumerator
        ClearSafetyMessageAfterDelay()
    {
        yield return
            new WaitForSeconds(
                safetyMessageDuration
            );

        faultHUD.ClearSafetyMessage();

        safetyMessageCoroutine = null;
    }
}