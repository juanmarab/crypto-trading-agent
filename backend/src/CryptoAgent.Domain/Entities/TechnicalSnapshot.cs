using CryptoAgent.Domain.Enums;

namespace CryptoAgent.Domain.Entities;

/// <summary>
/// A frozen snapshot of all technical indicators at a specific moment,
/// used to audit the AI's reasoning at decision time.
/// </summary>
public class TechnicalSnapshot
{
    public Guid Id { get; set; }
    public CryptoAsset Asset { get; set; }
    public string Timeframe { get; set; } = "15m";
    public DateTimeOffset CapturedAt { get; set; } = DateTimeOffset.UtcNow;

    // OHLCV
    public decimal OpenPrice { get; set; }
    public decimal HighPrice { get; set; }
    public decimal LowPrice { get; set; }
    public decimal ClosePrice { get; set; }
    public decimal Volume { get; set; }

    // EMAs
    public decimal? Ema20 { get; set; }
    public decimal? Ema50 { get; set; }
    public decimal? Ema200 { get; set; }

    // RSI
    public decimal? Rsi { get; set; }

    // MACD
    public decimal? MacdLine { get; set; }
    public decimal? MacdSignal { get; set; }
    public decimal? MacdHistogram { get; set; }

    // Bollinger Bands
    public decimal? BbUpper { get; set; }
    public decimal? BbMiddle { get; set; }
    public decimal? BbLower { get; set; }

    // ATR (Volatility)
    public decimal? Atr { get; set; }

    // Navigation
    public ICollection<AgentDecision> Decisions { get; set; } = [];
}
