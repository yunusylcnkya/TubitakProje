
using System;

[Flags]
public enum ElectricalPotentialFlags
{
    None = 0,

    DCPositive = 1 << 0,
    DCNegative = 1 << 1,

    PhaseL1 = 1 << 2,
    PhaseL2 = 1 << 3,
    PhaseL3 = 1 << 4,

    Neutral = 1 << 5,
    ProtectiveEarth = 1 << 6
}
