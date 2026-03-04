using CryptoAgent.Domain.Enums;

namespace CryptoAgent.Application.DTOs.TechnicalAnalysis;

/// <summary>A single OHLCV candlestick (Kline) from Binance.</summary>
public record KlineData
{
    public DateTimeOffset OpenTime  { get; init; }
    public DateTimeOffset CloseTime { get; init; }
    public decimal Open   { get; init; }
    public decimal High   { get; init; }
    public decimal Low    { get; init; }
    public decimal Close  { get; init; }
    public decimal Volume { get; init; }
}

/// <summary>Real-time price ticker for a crypto asset.</summary>
public record PriceTick
{
    public CryptoAsset Asset                { get; init; }
    public decimal Price                    { get; init; }
    public decimal Volume24h                { get; init; }
    public decimal PriceChangePercent24h    { get; init; }
    public DateTimeOffset Timestamp         { get; init; }
}

/// <summary>
/// Full set of calculated technical indicators for a single asset and timeframe,
/// including the nearest support and resistance levels derived from swing highs/lows.
/// </summary>
public record IndicatorSet
{
    public CryptoAsset Asset    { get; init; }
    public string Timeframe     { get; init; } = "15m";
    public DateTimeOffset Timestamp { get; init; }

    // OHLCV (latest candle)
    public decimal Open   { get; init; }
    public decimal High   { get; init; }
    public decimal Low    { get; init; }
    public decimal Close  { get; init; }
    public decimal Volume { get; init; }

    // EMAs
    public decimal? Ema20  { get; init; }
    public decimal? Ema50  { get; init; }
    public decimal? Ema200 { get; init; }

    // RSI
    public decimal? Rsi { get; init; }

    // MACD
    public decimal? MacdLine      { get; init; }
    public decimal? MacdSignal    { get; init; }
    public decimal? MacdHistogram { get; init; }

    // Bollinger Bands
    public decimal? BbUpper  { get; init; }
    public decimal? BbMiddle { get; init; }
    public decimal? BbLower  { get; init; }

    // ATR
    public decimal? Atr { get; init; }

    // Derived signals
    public string? EmaTrend       { get; init; }  // BULLISH | BEARISH | NEUTRAL
    public string? RsiSignal      { get; init; }  // OVERBOUGHT | OVERSOLD | NEUTRAL
    public string? MacdSignalType { get; init; }  // BULLISH_CROSS | BEARISH_CROSS | NEUTRAL
    public string? BbPosition     { get; init; }  // ABOVE_UPPER | BELOW_LOWER | WITHIN

    // ── Support & Resistance (swing-based) ───────────────────────────────
    public decimal? Support1    { get; init; }
    public decimal? Support2    { get; init; }
    public decimal? Resistance1 { get; init; }
    public decimal? Resistance2 { get; init; }
}

/// <summary>Dashboard-ready technical analysis summary for a single asset.</summary>
public record TechnicalAnalysisDto
{
    public CryptoAsset Asset           { get; init; }
    public PriceTick?  CurrentPrice    { get; init; }
    public IndicatorSet? Indicators5m  { get; init; }
    public IndicatorSet? Indicators15m { get; init; }
    public string OverallVerdict       { get; init; } = "NEUTRAL";
    public DateTimeOffset LastUpdated  { get; init; }
}
