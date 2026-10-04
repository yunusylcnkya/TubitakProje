using System;
using UnityEngine;

[Serializable]
public sealed class RequiredConnection
{
    [SerializeField]
    private string terminalIdA;

    [SerializeField]
    private string terminalIdB;

    public string TerminalIdA =>
        terminalIdA?.Trim();

    public string TerminalIdB =>
        terminalIdB?.Trim();

    public bool Matches(
        string idA,
        string idB)
    {
        if (string.IsNullOrWhiteSpace(idA) ||
            string.IsNullOrWhiteSpace(idB))
        {
            return false;
        }

        string actualA =
            idA.Trim();

        string actualB =
            idB.Trim();

        bool forward =
            string.Equals(
                TerminalIdA,
                actualA,
                StringComparison.Ordinal
            )
            &&
            string.Equals(
                TerminalIdB,
                actualB,
                StringComparison.Ordinal
            );

        bool reverse =
            string.Equals(
                TerminalIdA,
                actualB,
                StringComparison.Ordinal
            )
            &&
            string.Equals(
                TerminalIdB,
                actualA,
                StringComparison.Ordinal
            );

        return forward || reverse;
    }
}