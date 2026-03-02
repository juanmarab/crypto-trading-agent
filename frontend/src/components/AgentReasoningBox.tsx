import type { AgentDecision, TradeAction } from '../types';
import { TrendingUp, TrendingDown, Minus, Zap, Brain, Newspaper, AlertCircle } from 'lucide-react';

interface Props {
    decision?: AgentDecision;
    loading?: boolean;
}

type ActionConfig = { label: string; class: string; Icon: React.ElementType };

const ACTION_CONFIG: Record<TradeAction, ActionConfig> = {
    LONG: { label: 'LONG', class: 'action-long', Icon: TrendingUp },
    SHORT: { label: 'SHORT', class: 'action-short', Icon: TrendingDown },
    HOLD: { label: 'HOLD', class: 'action-hold', Icon: Minus },
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

    const cfg: ActionConfig = ACTION_CONFIG[decision.action] ?? ACTION_CONFIG.HOLD;
    const confidence = Math.round((decision.confidence ?? 0) * 100);
    const confColor = confidence >= 75 ? '#22c55e' : confidence >= 50 ? '#f59e0b' : '#ef4444';

    return (
        <div className="glass-box panel-reasoning">
            <div className="reasoning-header">
                <Brain size={18} className="icon-accent" />
                <h3>AI Reasoning</h3>
                <span className="reasoning-time">
                    {new Date(decision.decidedAt).toLocaleTimeString()}
                </span>
            </div>

            <div className={`action-badge ${cfg.class}`}>
                <cfg.Icon size={22} />
                <span>{cfg.label}</span>
                {decision.suggestedLeverage && (
                    <span className="leverage-pill">
                        <Zap size={12} /> {decision.suggestedLeverage}x
                    </span>
                )}
            </div>

            <div className="confidence-section">
                <div className="conf-label">
                    <span>Confidence</span>
                    <span style={{ color: confColor }}>{confidence}%</span>
                </div>
                <div className="conf-bar-track">
                    <div
                        className="conf-bar-fill"
                        style={{ width: `${confidence}%`, background: confColor }}
                    />
                </div>
            </div>

            <div className="verdicts-grid">
                <VerdictBadge label="Technical" value={decision.technicalVerdict} />
                <VerdictBadge label="Fundamental" value={decision.fundamentalVerdict} />
            </div>

            <div className="reasoning-footer">
                <Newspaper size={12} />
                <span>Decision ID: {decision.id.slice(0, 8)}…</span>
            </div>
        </div>
    );
}
