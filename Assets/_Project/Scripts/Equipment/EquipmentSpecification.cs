using System;
using UnityEngine;

[Serializable]
public sealed class EquipmentSpecification
{
    [SerializeField]
    private string label;

    [SerializeField]
    private string value;

    public string Label => label;
    public string Value => value;
}