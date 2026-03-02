// ── Shared domain types mirroring the backend DTOs ───────────────────────────

export type CryptoAsset = 'BTC' | 'ETH' | 'SOL' | 'BNB';
export type TradeAction = 'LONG' | 'SHORT' | 'HOLD';

export interface PriceTick {
    asset: CryptoAsset;
    price: number;
    volume24h: number;
    priceChangePercent24h: number;
    timestamp: string;
}

export interface KlineData {
    openTime: string;
    closeTime: string;
    open: number;
    high: number;
    low: number;
    close: number;
    volume: number;
}

export interface IndicatorSet {
    asset: CryptoAsset;
    timeframe: string;
    timestamp: string;
    open: number; high: number; low: number; close: number; volume: number;
    ema20?: number; ema50?: number; ema200?: number;
    rsi?: number;
    macdLine?: number; macdSignal?: number; macdHistogram?: number;
    bbUpper?: number; bbMiddle?: number; bbLower?: number;
    atr?: number;
    emaTrend?: string;
    rsiSignal?: string;
    macdSignalType?: string;
    bbPosition?: string;
}

export interface TechnicalAnalysisDto {
    asset: CryptoAsset;
    currentPrice?: PriceTick;
    indicators5m?: IndicatorSet;
    indicators15m?: IndicatorSet;
    overallVerdict: string;
    lastUpdated: string;
}

export interface AgentDecision {
    id: string;
    asset: CryptoAsset;
    decidedAt: string;
    technicalVerdict: string;
    fundamentalVerdict: string;
    action: TradeAction;
    suggestedLeverage?: number;
    confidence: number;
    snapshotId?: string;
}

export interface MarketNewsItem {
    id: string;
    asset: CryptoAsset;
    headline: string;
    content?: string;
    sourceUrl?: string;
    publishedAt: string;
    sentimentScore?: number;
    hasEmbedding: boolean;
}
