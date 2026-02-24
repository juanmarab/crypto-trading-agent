using CryptoAgent.Application.DTOs.TechnicalAnalysis;
using CryptoAgent.Application.Interfaces;
using CryptoAgent.Domain.Enums;

namespace CryptoAgent.Infrastructure.Services.TechnicalAnalysis;

/// <summary>
/// Pure mathematical indicator calculations.
/// No I/O, no side effects — completely deterministic.
/// </summary>
public class TechnicalAnalysisService : ITechnicalAnalysisService
{
    public IndicatorSet CalculateIndicators(CryptoAsset asset, string timeframe, IReadOnlyList<KlineData> klines)
    {
        if (klines.Count == 0)
            throw new ArgumentException("At least one kline is required.", nameof(klines));

        var closes = klines.Select(k => k.Close).ToArray();
        var highs = klines.Select(k => k.High).ToArray();
        var lows = klines.Select(k => k.Low).ToArray();
        var latest = klines[^1];

        // Calculate all indicators
        var ema20 = CalculateEma(closes, 20);
        var ema50 = CalculateEma(closes, 50);
        var ema200 = CalculateEma(closes, 200);
        var rsi = CalculateRsi(closes, 14);
        var (macdLine, macdSignal, macdHistogram) = CalculateMacd(closes);
        var (bbUpper, bbMiddle, bbLower) = CalculateBollingerBands(closes, 20, 2.0m);
        var atr = CalculateAtr(highs, lows, closes, 14);

        // Derive signals
        var emaTrend = DeriveEmaTrend(latest.Close, ema20, ema50, ema200);
        var rsiSignal = DeriveRsiSignal(rsi);
        var macdSignalType = DeriveMacdSignal(macdLine, macdSignal, macdHistogram, closes);
        var bbPosition = DeriveBbPosition(latest.Close, bbUpper, bbLower);

        return new IndicatorSet
        {
            Asset = asset,
            Timeframe = timeframe,
            Timestamp = latest.CloseTime,
            Open = latest.Open,
            High = latest.High,
            Low = latest.Low,
            Close = latest.Close,
            Volume = latest.Volume,
            Ema20 = ema20,
            Ema50 = ema50,
            Ema200 = ema200,
            Rsi = rsi,
            MacdLine = macdLine,
            MacdSignal = macdSignal,
            MacdHistogram = macdHistogram,
            BbUpper = bbUpper,
            BbMiddle = bbMiddle,
            BbLower = bbLower,
            Atr = atr,
            EmaTrend = emaTrend,
            RsiSignal = rsiSignal,
            MacdSignalType = macdSignalType,
            BbPosition = bbPosition
        };
    }

    // ── EMA ────────────────────────────────────────────────────────────────

    private static decimal? CalculateEma(decimal[] data, int period)
    {
        if (data.Length < period) return null;

        decimal multiplier = 2.0m / (period + 1);

        // Seed with SMA
        decimal ema = data.Take(period).Average();

        // Apply EMA formula
        for (int i = period; i < data.Length; i++)
        {
            ema = (data[i] - ema) * multiplier + ema;
        }

        return Math.Round(ema, 8);
    }

    // ── RSI ────────────────────────────────────────────────────────────────

    private static decimal? CalculateRsi(decimal[] data, int period)
    {
        if (data.Length < period + 1) return null;

        decimal avgGain = 0, avgLoss = 0;

        // Initial average gain/loss
        for (int i = 1; i <= period; i++)
        {
            var change = data[i] - data[i - 1];
            if (change >= 0) avgGain += change;
            else avgLoss += Math.Abs(change);
        }
        avgGain /= period;
        avgLoss /= period;

        // Smoothed average (Wilder's smoothing)
        for (int i = period + 1; i < data.Length; i++)
        {
            var change = data[i] - data[i - 1];
            if (change >= 0)
            {
                avgGain = (avgGain * (period - 1) + change) / period;
                avgLoss = (avgLoss * (period - 1)) / period;
            }
            else
            {
                avgGain = (avgGain * (period - 1)) / period;
                avgLoss = (avgLoss * (period - 1) + Math.Abs(change)) / period;
            }
        }

        if (avgLoss == 0) return 100m;

        var rs = avgGain / avgLoss;
        return Math.Round(100m - (100m / (1m + rs)), 4);
    }

    // ── MACD ──────────────────────────────────────────────────────────────

    private static (decimal? line, decimal? signal, decimal? histogram) CalculateMacd(decimal[] data)
    {
        if (data.Length < 26) return (null, null, null);

        // MACD Line = EMA(12) - EMA(26)
        var ema12Values = CalculateEmaArray(data, 12);
        var ema26Values = CalculateEmaArray(data, 26);

        if (ema12Values == null || ema26Values == null) return (null, null, null);

        // Build MACD line series (starting from index 25 where both EMAs exist)
        var macdValues = new List<decimal>();
        int startIdx = 25; // 26 - 1 (0-indexed)
        for (int i = startIdx; i < data.Length; i++)
        {
            macdValues.Add(ema12Values[i] - ema26Values[i]);
        }

        if (macdValues.Count < 9) return (macdValues[^1], null, null);

        // Signal Line = 9-period EMA of MACD Line
        var signalValues = CalculateEmaArray(macdValues.ToArray(), 9);
        if (signalValues == null) return (macdValues[^1], null, null);

        var macdLine = Math.Round(macdValues[^1], 8);
        var signalLine = Math.Round(signalValues[^1], 8);
        var histogram = Math.Round(macdLine - signalLine, 8);

        return (macdLine, signalLine, histogram);
    }

    private static decimal[]? CalculateEmaArray(decimal[] data, int period)
    {
        if (data.Length < period) return null;

        var result = new decimal[data.Length];
        decimal multiplier = 2.0m / (period + 1);

        // Seed with SMA
        result[period - 1] = data.Take(period).Average();

        // Fill earlier values with 0 (unused)
        for (int i = 0; i < period - 1; i++)
            result[i] = 0;

        for (int i = period; i < data.Length; i++)
        {
            result[i] = (data[i] - result[i - 1]) * multiplier + result[i - 1];
        }

        return result;
    }

    // ── Bollinger Bands ───────────────────────────────────────────────────

    private static (decimal? upper, decimal? middle, decimal? lower) CalculateBollingerBands(
        decimal[] data, int period, decimal stdDevMultiplier)
    {
        if (data.Length < period) return (null, null, null);

        var slice = data[^period..];
        var sma = slice.Average();
        var variance = slice.Sum(v => (v - sma) * (v - sma)) / period;
        var stdDev = (decimal)Math.Sqrt((double)variance);

        return (
            Math.Round(sma + stdDevMultiplier * stdDev, 8),
            Math.Round(sma, 8),
            Math.Round(sma - stdDevMultiplier * stdDev, 8)
        );
    }

    // ── ATR ────────────────────────────────────────────────────────────────

    private static decimal? CalculateAtr(decimal[] highs, decimal[] lows, decimal[] closes, int period)
    {
        if (highs.Length < period + 1) return null;

        var trValues = new decimal[highs.Length - 1];
        for (int i = 1; i < highs.Length; i++)
        {
            var hl = highs[i] - lows[i];
            var hc = Math.Abs(highs[i] - closes[i - 1]);
            var lc = Math.Abs(lows[i] - closes[i - 1]);
            trValues[i - 1] = Math.Max(hl, Math.Max(hc, lc));
        }

        if (trValues.Length < period) return null;

        // Initial ATR = average of first 'period' TRs
        decimal atr = trValues.Take(period).Average();

        // Smoothed ATR (Wilder's method)
        for (int i = period; i < trValues.Length; i++)
        {
            atr = (atr * (period - 1) + trValues[i]) / period;
        }

        return Math.Round(atr, 8);
    }

    // ── Signal Derivation ─────────────────────────────────────────────────

    private static string DeriveEmaTrend(decimal currentPrice, decimal? ema20, decimal? ema50, decimal? ema200)
    {
        if (ema20 == null || ema50 == null) return "NEUTRAL";

        // Strong bullish: Price > EMA20 > EMA50 (> EMA200 if available)
        if (currentPrice > ema20 && ema20 > ema50)
        {
            if (ema200 != null && ema50 > ema200) return "STRONG_BULLISH";
            return "BULLISH";
        }

        // Strong bearish: Price < EMA20 < EMA50
        if (currentPrice < ema20 && ema20 < ema50)
        {
            if (ema200 != null && ema50 < ema200) return "STRONG_BEARISH";
            return "BEARISH";
        }

        return "NEUTRAL";
    }

    private static string DeriveRsiSignal(decimal? rsi)
    {
        if (rsi == null) return "NEUTRAL";
        if (rsi >= 70) return "OVERBOUGHT";
        if (rsi <= 30) return "OVERSOLD";
        return "NEUTRAL";
    }

    private static string DeriveMacdSignal(decimal? macdLine, decimal? macdSignal, decimal? histogram, decimal[] closes)
    {
        if (macdLine == null || macdSignal == null || histogram == null) return "NEUTRAL";

        // Check for crossover by comparing current and previous histogram direction
        if (histogram > 0 && macdLine > macdSignal) return "BULLISH_CROSS";
        if (histogram < 0 && macdLine < macdSignal) return "BEARISH_CROSS";

        return "NEUTRAL";
    }

    private static string DeriveBbPosition(decimal currentPrice, decimal? bbUpper, decimal? bbLower)
    {
        if (bbUpper == null || bbLower == null) return "WITHIN";
        if (currentPrice >= bbUpper) return "ABOVE_UPPER";
        if (currentPrice <= bbLower) return "BELOW_LOWER";
        return "WITHIN";
    }
}
