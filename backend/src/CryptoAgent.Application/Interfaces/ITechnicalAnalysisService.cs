using CryptoAgent.Application.DTOs.TechnicalAnalysis;
using CryptoAgent.Domain.Enums;

namespace CryptoAgent.Application.Interfaces;

/// <summary>
/// Service for calculating technical indicators from raw OHLCV data.
/// Pure math — no I/O, no side effects.
/// </summary>
public interface ITechnicalAnalysisService
{
    IndicatorSet CalculateIndicators(CryptoAsset asset, string timeframe, IReadOnlyList<KlineData> klines);
}
