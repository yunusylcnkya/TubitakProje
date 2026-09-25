using UnityEngine;

public sealed class WireSegment : MonoBehaviour
{
    private Wire owner;

    public Wire Owner => owner;

    public void Initialize(Wire wire)
    {
        owner = wire;
    }
}