using CryptoAgent.Application.DTOs.TechnicalAnalysis;

namespace CryptoAgent.Infrastructure.Services.TechnicalAnalysis;

/// <summary>
/// Detects the nearest swing-based support and resistance levels from a kline series.
///
/// Algorithm:
///   Swing High = a bar whose High is the highest among (N bars before + N bars after) → resistance
///   Swing Low  = a bar whose Low  is the lowest  among (N bars before + N bars after) → support
///
/// We return the two nearest support levels below close and
/// the two nearest resistance levels above close.
/// </summary>
public static class SupportResistanceCalculator
{
    /// <summary>
    /// Find nearest supports (below close) and resistances (above close).
    /// </summary>
    public static (decimal? S1, decimal? S2, decimal? R1, decimal? R2)
        Calculate(IReadOnlyList<KlineData> klines, int lookback = 5)
    {
        if (klines.Count < lookback * 2 + 1)
            return (null, null, null, null);

        decimal close = klines[^1].Close;

        var swingHighs = new List<decimal>();
        var swingLows  = new List<decimal>();

        // Scan all bars except the last `lookback` (need right-side confirmation)
        int end = klines.Count - lookback;
        for (int i = lookback; i < end; i++)
        {
            decimal hi = klines[i].High;
            decimal lo = klines[i].Low;
            bool isSwingHigh = true, isSwingLow = true;

            for (int j = i - lookback; j <= i + lookback; j++)
            {
                if (j == i) continue;
                if (klines[j].High >= hi) isSwingHigh = false;
                if (klines[j].Low  <= lo) isSwingLow  = false;
                if (!isSwingHigh && !isSwingLow) break;
            }

            if (isSwingHigh) swingHighs.Add(Math.Round(hi, 4));
            if (isSwingLow)  swingLows.Add(Math.Round(lo, 4));
        }

        // Supports = swing lows below current close, sorted descending (nearest first)
        var supports = swingLows
            .Where(v => v < close)
            .OrderByDescending(v => v)
            .Distinct()
            .Take(2)
            .ToList();

        // Resistances = swing highs above current close, sorted ascending (nearest first)
        var resistances = swingHighs
            .Where(v => v > close)
            .OrderBy(v => v)
            .Distinct()
            .Take(2)
            .ToList();

        return (
            supports.Count     > 0 ? supports[0]     : null,
            supports.Count     > 1 ? supports[1]     : null,
            resistances.Count  > 0 ? resistances[0]  : null,
            resistances.Count  > 1 ? resistances[1]  : null
        );
    }
}
