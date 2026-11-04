
using UnityEngine;

public enum ConnectionValidationMode
{
    Training,
    Strict
}

public enum ConnectionIssueSeverity
{
    None,
    Warning,
    Danger
}

public readonly struct ConnectionValidationResult
{
    public ConnectionIssueSeverity Severity { get; }
    public string Message { get; }
    public bool IsAllowed { get; }

    public ConnectionValidationResult(
        ConnectionIssueSeverity severity,
        string message,
        bool isAllowed)
    {
        Severity = severity;
        Message = message;
        IsAllowed = isAllowed;
    }
}

public static class ConnectionValidator
{
    public static ConnectionValidationResult Evaluate(
        Terminal terminalA,
        Terminal terminalB,
        ElectricalSimulationSnapshot snapshot,
        ConnectionValidationMode mode)
    {
        if (terminalA == null || terminalB == null)
        {
            return Invalid("Terminal referansı eksik.");
        }

        if (terminalA == terminalB)
        {
            return Invalid(
                "Bir terminal kendisine bağlanamaz."
            );
        }

        // Öncelikle gerçek elektriksel potansiyeller.
        ElectricalPotentialFlags potentialA =
            snapshot != null
                ? snapshot.GetPotential(terminalA)
                : ElectricalPotentialFlags.None;

        ElectricalPotentialFlags potentialB =
            snapshot != null
                ? snapshot.GetPotential(terminalB)
                : ElectricalPotentialFlags.None;

        // Her iki tarafta da bilinen potansiyel varsa
        // öncelikle bunların birleşimini değerlendir.
        if (potentialA != ElectricalPotentialFlags.None &&
            potentialB != ElectricalPotentialFlags.None)
        {
            ConnectionIssueSeverity severity =
                CheckPotentialCombination(
                    potentialA | potentialB,
                    out string reason
                );

            if (severity != ConnectionIssueSeverity.None)
            {
                return Build(mode, severity, reason);
            }

            // Potansiyeller çakışmıyorsa terminal
            // etiketindeki farklılık tek başına
            // bağlantıyı engellememelidir.
            if (terminalA.LineType != terminalB.LineType &&
                IsPowerLine(terminalA.LineType) &&
                IsPowerLine(terminalB.LineType))
            {
                return Build(
                    mode,
                    ConnectionIssueSeverity.Warning,
                    "Terminal hat türleri farklı; ancak " +
                    "mevcut potansiyellerde doğrudan " +
                    "çakışma tespit edilmedi. " +
                    "Terminal tanımlarını kontrol edin."
                );
            }

            return Valid();
        }

        // Enerji yoksa veya potansiyel bilinmiyorsa
        // terminalin tanımlanmış hat türlerinden yararlan.
        return CheckLineTypes(
            terminalA.LineType,
            terminalB.LineType,
            mode
        );
    }

    private static ConnectionIssueSeverity
        CheckPotentialCombination(
            ElectricalPotentialFlags potential,
            out string reason)
    {
        reason = string.Empty;

        bool l1 = Has(
            potential, ElectricalPotentialFlags.PhaseL1);
        bool l2 = Has(
            potential, ElectricalPotentialFlags.PhaseL2);
        bool l3 = Has(
            potential, ElectricalPotentialFlags.PhaseL3);
        bool neutral = Has(
            potential, ElectricalPotentialFlags.Neutral);
        bool earth = Has(
            potential, ElectricalPotentialFlags.ProtectiveEarth);
        bool dcPositive = Has(
            potential, ElectricalPotentialFlags.DCPositive);
        bool dcNegative = Has(
            potential, ElectricalPotentialFlags.DCNegative);

        int phaseCount =
            (l1 ? 1 : 0) +
            (l2 ? 1 : 0) +
            (l3 ? 1 : 0);

        if (phaseCount > 1)
        {
            reason =
                "Farklı AC fazları doğrudan birleştiriliyor.";
            return ConnectionIssueSeverity.Danger;
        }

        if (phaseCount > 0 && neutral)
        {
            reason =
                "Faz ile nötr arasında doğrudan bağlantı " +
                "oluşturuluyor. Kısa devre riski.";
            return ConnectionIssueSeverity.Danger;
        }

        if (phaseCount > 0 && earth)
        {
            reason =
                "Faz ile koruma topraklaması " +
                "doğrudan birleştiriliyor.";
            return ConnectionIssueSeverity.Danger;
        }

        if (phaseCount > 0 && (dcPositive || dcNegative))
        {
            reason =
                "AC faz hattı ile DC hattı " +
                "doğrudan birleştiriliyor.";
            return ConnectionIssueSeverity.Danger;
        }

        if (dcPositive && dcNegative)
        {
            reason =
                "DC pozitif ve negatif hatları " +
                "doğrudan birleştiriliyor.";
            return ConnectionIssueSeverity.Danger;
        }

        if (neutral && dcPositive)
        {
            reason =
                "AC nötr ile DC pozitif hattı " +
                "doğrudan birleştiriliyor.";
            return ConnectionIssueSeverity.Danger;
        }

        if (earth && dcPositive)
        {
            reason =
                "DC pozitif hattı koruma " +
                "topraklamasına bağlanıyor.";
            return ConnectionIssueSeverity.Danger;
        }

        if (neutral && earth)
        {
            reason =
                "Nötr ve PE birleştiriliyor. " +
                "Bu bağlantı yalnızca uygun " +
                "topraklama tasarımında yapılmalıdır.";
            return ConnectionIssueSeverity.Warning;
        }

        if (earth && dcNegative)
        {
            reason =
                "DC 0V ile PE bağlantısı " +
                "oluşturuluyor. Sistem tasarımını kontrol edin.";
            return ConnectionIssueSeverity.Warning;
        }

        if (neutral && dcNegative)
        {
            reason =
                "AC nötr ile DC 0V birleştiriliyor. " +
                "İzolasyon ve topraklama tasarımını kontrol edin.";
            return ConnectionIssueSeverity.Warning;
        }

        return ConnectionIssueSeverity.None;
    }

    private static ConnectionValidationResult CheckLineTypes(
        ElectricalLineType a,
        ElectricalLineType b,
        ConnectionValidationMode mode)
    {
        if (a == b)
            return Valid();

        // Genel amaçlı terminaller farklı hatları
        // taşıyabilir. Tür bilgisi yeterli değildir.
        if (a == ElectricalLineType.Undefined ||
            b == ElectricalLineType.Undefined ||
            a == ElectricalLineType.Control ||
            b == ElectricalLineType.Control)
        {
            return Valid();
        }

        if (IsSignalLine(a) || IsSignalLine(b))
        {
            return Build(
                mode,
                ConnectionIssueSeverity.Warning,
                $"Sinyal bağlantısını kontrol edin: {a} / {b}. " +
                "Gerilim ve giriş tipi uyumlu olmalıdır."
            );
        }

        if (IsPair(
            a, b,
            ElectricalLineType.Neutral,
            ElectricalLineType.ProtectiveEarth))
        {
            return Build(
                mode,
                ConnectionIssueSeverity.Warning,
                "N ve PE bağlantısı tasarıma bağlıdır. " +
                "Doğru bağlantı noktasını kontrol edin."
            );
        }

        if (IsPair(
            a, b,
            ElectricalLineType.DCNegative,
            ElectricalLineType.ProtectiveEarth))
        {
            return Build(
                mode,
                ConnectionIssueSeverity.Warning,
                "0V DC ve PE bağlantısı için " +
                "topraklama tasarımını kontrol edin."
            );
        }

        if (IsPair(
            a, b,
            ElectricalLineType.Neutral,
            ElectricalLineType.DCNegative))
        {
            return Build(
                mode,
                ConnectionIssueSeverity.Warning,
                "AC nötr ve DC 0V bağlantısı " +
                "izolasyon açısından incelenmelidir."
            );
        }

        return Build(
            mode,
            ConnectionIssueSeverity.Danger,
            $"Uyumsuz güç hattı türleri: {a} / {b}. " +
            "Bağlantıyı kontrol edin."
        );
    }

    private static bool Has(
        ElectricalPotentialFlags value,
        ElectricalPotentialFlags flag)
    {
        return (value & flag) != 0;
    }

    private static bool IsPair(
        ElectricalLineType a,
        ElectricalLineType b,
        ElectricalLineType x,
        ElectricalLineType y)
    {
        return (a == x && b == y) ||
               (a == y && b == x);
    }

    private static bool IsSignalLine(ElectricalLineType type)
    {
        return type == ElectricalLineType.DigitalSignal ||
               type == ElectricalLineType.AnalogSignal;
    }

    private static bool IsPowerLine(ElectricalLineType type)
    {
        return type == ElectricalLineType.PhaseL1 ||
               type == ElectricalLineType.PhaseL2 ||
               type == ElectricalLineType.PhaseL3 ||
               type == ElectricalLineType.Neutral ||
               type == ElectricalLineType.ProtectiveEarth ||
               type == ElectricalLineType.DCPositive ||
               type == ElectricalLineType.DCNegative;
    }

    private static ConnectionValidationResult Build(
        ConnectionValidationMode mode,
        ConnectionIssueSeverity severity,
        string message)
    {
        bool allowed =
            severity != ConnectionIssueSeverity.Danger ||
            mode == ConnectionValidationMode.Training;

        return new ConnectionValidationResult(
            severity,
            message,
            allowed
        );
    }

    private static ConnectionValidationResult Valid()
    {
        return new ConnectionValidationResult(
            ConnectionIssueSeverity.None,
            "Bağlantı uygun.",
            true
        );
    }

    private static ConnectionValidationResult Invalid(
        string message)
    {
        return new ConnectionValidationResult(
            ConnectionIssueSeverity.Danger,
            message,
            false
        );
    }
}
