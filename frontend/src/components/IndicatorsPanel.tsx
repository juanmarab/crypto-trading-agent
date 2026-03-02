import type { IndicatorSet } from '../types';
import { Activity } from 'lucide-react';

interface Props { indicators?: IndicatorSet; loading?: boolean; }

function Row({ label, value, signal }: { label: string; value?: string | number; signal?: string }) {
    const bull = signal?.includes('BULLISH') || signal?.includes('OVERSOLD') || signal?.includes('BELOW');
    const bear = signal?.includes('BEARISH') || signal?.includes('OVERBOUGHT') || signal?.includes('ABOVE');
    return (
        <div className="indicator-row">
            <span className="ind-label">{label}</span>
            <span className="ind-value">{value ?? '—'}</span>
            {signal && <span className={`ind-signal ${bull ? 'bull' : bear ? 'bear' : 'neutral'}`}>{signal}</span>}
        </div>
    );
}

function fmt(n?: number, d = 2): string | undefined { return n != null ? n.toFixed(d) : undefined; }

export default function IndicatorsPanel({ indicators, loading }: Props) {
    return (
        <div className="glass-box panel-indicators">
            <div className="panel-header">
                <Activity size={16} className="icon-accent" />
                <h3>Indicators <span className="tf-badge">{indicators?.timeframe ?? '15m'}</span></h3>
            </div>

            {loading ? (
                <div className="ind-skeleton">{[1, 2, 3, 4, 5].map(i => <div key={i} className="skeleton-line" />)}</div>
            ) : !indicators ? (
                <p className="empty-ind">Waiting for data…</p>
            ) : (
                <div className="indicators-list">
                    <div className="ind-section-label">Price</div>
                    <Row label="Close" value={fmt(indicators.close, 4)} />
                    <Row label="Volume" value={fmt(indicators.volume, 0)} />

                    <div className="ind-section-label">Trend</div>
                    <Row label="EMA 20" value={fmt(indicators.ema20, 4)} />
                    <Row label="EMA 50" value={fmt(indicators.ema50, 4)} />
                    <Row label="EMA 200" value={fmt(indicators.ema200, 4)} signal={indicators.emaTrend} />

                    <div className="ind-section-label">Momentum</div>
                    <Row label="RSI (14)" value={fmt(indicators.rsi, 2)} signal={indicators.rsiSignal} />
                    <Row label="MACD" value={fmt(indicators.macdLine, 6)} signal={indicators.macdSignalType} />
                    <Row label="Histogram" value={fmt(indicators.macdHistogram, 6)} />

                    <div className="ind-section-label">Volatility</div>
                    <Row label="BB Upper" value={fmt(indicators.bbUpper, 4)} />
                    <Row label="BB Lower" value={fmt(indicators.bbLower, 4)} signal={indicators.bbPosition} />
                    <Row label="ATR (14)" value={fmt(indicators.atr, 4)} />
                </div>
            )}
        </div>
    );
}
