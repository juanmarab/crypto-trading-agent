import type { AgentDecision, TradeAction } from '../types';
import {
    TrendingUp, TrendingDown, Minus, Zap, Brain,
    Target, ShieldAlert, Clock, FileText, AlertCircle,
    Send, DollarSign
} from 'lucide-react';

interface Props {
    decision?: AgentDecision;
    loading?: boolean;
}

type ActionConfig = { label: string; cls: string; Icon: React.ElementType };

const ACTION_CONFIG: Record<TradeAction, ActionConfig> = {
    LONG: { label: 'LONG', cls: 'action-long', Icon: TrendingUp },
    SHORT: { label: 'SHORT', cls: 'action-short', Icon: TrendingDown },
    HOLD: { label: 'HOLD', cls: 'action-hold', Icon: Minus },
};

function VerdictBadge({ label, value }: { label: string; value: string }) {
    const bull = value?.includes('BULLISH') || value?.includes('OVERSOLD');
    const bear = value?.includes('BEARISH') || value?.includes('OVERBOUGHT');
    return (
        <div className="verdict-badge">
            <span className="verdict-label">{label}</span>
            <span className={`verdict-value ${bull ? 'bull' : bear ? 'bear' : ''}`}>{value || '—'}</span>
        </div>
    );
}

function PxRow({ icon: Icon, label, value, cls }: {
    icon: React.ElementType; label: string; value?: number; cls: string
}) {
    const fmt = (n: number) =>
        n >= 1000
            ? `$${n.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`
            : `$${n.toFixed(4)}`;
    return (
        <div className={`price-row ${cls}`}>
            <Icon size={13} />
            <span className="price-row-label">{label}</span>
            <span className="price-row-value">{value != null ? fmt(value) : '—'}</span>
        </div>
    );
}

function ConfluenceMeter({ score }: { score: number }) {
    const color = score >= 8 ? '#22c55e' : score >= 5 ? '#f59e0b' : '#ef4444';
    const label = score >= 8 ? 'High Probability' : score >= 5 ? 'Moderate' : 'Low Signal';
    return (
        <div className="confluence-section">
            <div className="confluence-header">
                <span className="confluence-label">Confluence</span>
                <span className="confluence-score" style={{ color }}>{score}/10</span>
                <span className="confluence-tag" style={{ background: `${color}22`, color, border: `1px solid ${color}44` }}>
                    {label}
                </span>
            </div>
            <div className="confluence-bar-track">
                <div className="confluence-bar-fill" style={{ width: `${score * 10}%`, background: color }} />
            </div>
        </div>
    );
}

export default function AgentReasoningBox({ decision, loading }: Props) {
    if (loading) {
        return (
            <div className="glass-box panel-reasoning skeleton">
                <div className="skeleton-line" /><div className="skeleton-line short" />
            </div>
        );
    }

    if (!decision) {
        return (
            <div className="glass-box panel-reasoning empty-state">
                <AlertCircle size={28} className="muted-icon" />
                <p>No AI decision yet. The agent runs every 15 minutes.</p>
            </div>
        );
    }

    const cfg = ACTION_CONFIG[decision.action] ?? ACTION_CONFIG.HOLD;
    const confidence = Math.round((decision.confidence ?? 0) * 100);
    const confColor = confidence >= 75 ? '#22c55e' : confidence >= 50 ? '#f59e0b' : '#ef4444';
    const isActive = decision.action !== 'HOLD';

    return (
        <div className="glass-box panel-reasoning">
            {/* ── Header ─────────────────────────────────── */}
            <div className="reasoning-header">
                <Brain size={18} className="icon-accent" />
                <h3>AI Quant Analysis</h3>
                <span className="reasoning-time">
                    {new Date(decision.decidedAt).toLocaleTimeString()}
                </span>
            </div>

            {/* ── Action Badge ─────────────────────────────── */}
            <div className={`action-badge ${cfg.cls}`}>
                <cfg.Icon size={20} />
                <span>{cfg.label}</span>
                {decision.suggestedLeverage && (
                    <span className="leverage-pill"><Zap size={11} /> {decision.suggestedLeverage}x</span>
                )}
                {decision.holdingPeriodHours && (
                    <span className="hold-pill"><Clock size={11} /> ~{decision.holdingPeriodHours}h</span>
                )}
                {decision.sendTelegramAlert && (
                    <span className="telegram-pill"><Send size={11} /> Alert</span>
                )}
            </div>

            {/* ── Confluence Meter ─────────────────────────── */}
            {decision.confluenceScore != null && (
                <ConfluenceMeter score={decision.confluenceScore} />
            )}

            {/* ── Confidence Bar ───────────────────────────── */}
            <div className="confidence-section">
                <div className="conf-label">
                    <span>Confidence</span>
                    <span style={{ color: confColor }}>{confidence}%</span>
                </div>
                <div className="conf-bar-track">
                    <div className="conf-bar-fill" style={{ width: `${confidence}%`, background: confColor }} />
                </div>
            </div>

            {/* ── Trade Parameters ─────────────────────────── */}
            {isActive && (
                <div className="trade-params-grid">
                    <PxRow icon={Target} label="Entry" value={decision.entryPrice} cls="entry" />
                    <PxRow icon={Target} label="TP 1 (1:1.5)" value={decision.takeProfit} cls="tp" />
                    <PxRow icon={Target} label="TP 2 (1:3)" value={decision.takeProfit2} cls="tp2" />
                    <PxRow icon={ShieldAlert} label="Stop Loss" value={decision.stopLoss} cls="sl" />
                    {decision.positionSizeUsd != null && (
                        <div className="price-row pos-size">
                            <DollarSign size={13} />
                            <span className="price-row-label">Position Size</span>
                            <span className="price-row-value pos-size-value">
                                ${decision.positionSizeUsd.toFixed(2)} USD
                            </span>
                        </div>
                    )}
                </div>
            )}

            {/* ── Verdicts ─────────────────────────────────── */}
            <div className="verdicts-grid">
                <VerdictBadge label="Technical" value={decision.technicalVerdict} />
                <VerdictBadge label="Fundamental" value={decision.fundamentalVerdict} />
            </div>

            {/* ── Technical Reasoning ──────────────────────── */}
            {decision.technicalReasoning && (
                <div className="reasoning-text-block">
                    <div className="reasoning-text-header">
                        <FileText size={12} /><span>Technical Analysis</span>
                    </div>
                    <p className="reasoning-text">{decision.technicalReasoning}</p>
                </div>
            )}

            {/* ── Footer ───────────────────────────────────── */}
            <div className="reasoning-footer">
                <span>Decision ID: {decision.id.slice(0, 8)}…</span>
                {decision.riskRewardRatio && (
                    <span className="rr-footer">R/R {decision.riskRewardRatio}</span>
                )}
            </div>
        </div>
    );
}
