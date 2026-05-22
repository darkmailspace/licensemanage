namespace LicenseManager.Infrastructure.Payments;

/// <summary>
/// Helpers for converting between major-unit decimal amounts (49.99 USD)
/// and the minor-unit longs (4999) that all the providers transact in.
/// Honours the ISO 4217 zero-decimal currency list so JPY 1000 stays 1000
/// and does not become 100000.
/// </summary>
internal static class MoneyUtil
{
    private static readonly HashSet<string> ZeroDecimalCurrencies = new(StringComparer.OrdinalIgnoreCase)
    {
        "BIF", "CLP", "DJF", "GNF", "JPY", "KMF", "KRW", "MGA", "PYG", "RWF",
        "UGX", "VND", "VUV", "XAF", "XOF", "XPF",
    };

    /// <summary>true if <paramref name="currency"/> has zero fractional digits per ISO 4217.</summary>
    public static bool IsZeroDecimal(string currency) => ZeroDecimalCurrencies.Contains(currency);

    /// <summary>Convert a major-unit decimal (e.g. 49.99) to a minor-unit long (e.g. 4999).</summary>
    public static long ToMinor(decimal majorAmount, string currency)
    {
        if (majorAmount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(majorAmount), "Amount must be non-negative.");
        }

        var multiplier = IsZeroDecimal(currency) ? 1m : 100m;
        return (long)Math.Round(majorAmount * multiplier, MidpointRounding.AwayFromZero);
    }

    /// <summary>Convert a minor-unit long back to a major-unit decimal.</summary>
    public static decimal ToMajor(long minorAmount, string currency)
    {
        var divisor = IsZeroDecimal(currency) ? 1m : 100m;
        return minorAmount / divisor;
    }
}
