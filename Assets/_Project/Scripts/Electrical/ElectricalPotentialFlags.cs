using System;

[Flags]
public enum ElectricalPotentialFlags
{
    None = 0,

    DCPositive = 1 << 0,

    DCNegative = 1 << 1
}