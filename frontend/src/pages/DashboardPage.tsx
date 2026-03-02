import { useEffect, useState } from 'react';
import { useStore } from '../store/useStore';
import type { KlineData } from '../types';
import { fetchKlines } from '../api/client';
import AssetSelector from '../components/AssetSelector';
import CandlestickChart from '../components/CandlestickChart';
import IndicatorsPanel from '../components/IndicatorsPanel';
import AgentReasoningBox from '../components/AgentReasoningBox';
import NewsFeed from '../components/NewsFeed';
import TelegramModal from '../components/TelegramModal';
import { Bell, Wifi, WifiOff, RefreshCw } from 'lucide-react';

export default function DashboardPage() {
    const {
        activeAsset, analysis, decision, news,
        loadingAnalysis, loadingDecision, loadingNews,
        hubStatus, startHub, loadAssetData,
    } = useStore();

    const [klines, setKlines] = useState<KlineData[]>([]);
    const [loadingChart, setLoadingChart] = useState(false);
    const [showTelegram, setShowTelegram] = useState(false);
    const [lastRefresh, setLastRefresh] = useState(new Date());

    const currentAnalysis = analysis[activeAsset];
    const currentDecision = decision[activeAsset];
    const currentNews = news[activeAsset] ?? [];

    useEffect(() => {
        startHub();
        loadAssetData(activeAsset);
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    useEffect(() => {
        setLoadingChart(true);
        fetchKlines(activeAsset, '15m', 200)
            .then(setKlines)
            .finally(() => setLoadingChart(false));
    }, [activeAsset]);

    const handleRefresh = () => {
        void loadAssetData(activeAsset);
        setLoadingChart(true);
        fetchKlines(activeAsset, '15m', 200)
            .then(setKlines)
            .finally(() => setLoadingChart(false));
        setLastRefresh(new Date());
    };

    return (
        <div className="dashboard">
            {/* ── Top Bar ─────────────────────────────────────────────── */}
            <header className="topbar">
                <div className="brand">
                    <span className="brand-icon">⬡</span>
                    <span className="brand-name">CryptoAgent</span>
                    <span className="brand-sub">AI Trading Assistant</span>
                </div>

                <AssetSelector />

                <div className="topbar-actions">
                    <div className={`hub-indicator ${hubStatus}`}>
                        {hubStatus === 'connected'
                            ? <><Wifi size={14} /> Live</>
                            : <><WifiOff size={14} /> {hubStatus === 'connecting' ? 'Connecting…' : 'Offline'}</>
                        }
                    </div>
                    <button className="icon-btn" title="Refresh data" onClick={handleRefresh}>
                        <RefreshCw size={16} className={loadingAnalysis ? 'spin' : ''} />
                    </button>
                    <button className="icon-btn alert-btn" title="Telegram Alerts" onClick={() => setShowTelegram(true)}>
                        <Bell size={16} />
                        <span>Alerts</span>
                    </button>
                </div>
            </header>

            {/* ── Main Layout ──────────────────────────────────────────── */}
            <main className="main-grid">
                {/* Column 1: Chart */}
                <section className="col-chart">
                    <div className="chart-header">
                        <h2 className="chart-title">{activeAsset}/USDT</h2>
                        <span className="chart-tf">15m • {klines.length} candles</span>
                        <span className="chart-updated">Updated {lastRefresh.toLocaleTimeString()}</span>
                    </div>

                    <div className="chart-wrapper">
                        {loadingChart
                            ? <div className="chart-loading"><span className="spin-large">⬡</span></div>
                            : <CandlestickChart klines={klines} indicators={currentAnalysis?.indicators15m} />
                        }
                    </div>

                    <div className="verdict-bar">
                        <span className="verdict-bar-label">Overall Verdict:</span>
                        <span className={`verdict-bar-value ${currentAnalysis?.overallVerdict === 'BULLISH' ? 'bull' :
                                currentAnalysis?.overallVerdict === 'BEARISH' ? 'bear' : 'neutral'
                            }`}>
                            {currentAnalysis?.overallVerdict ?? '…'}
                        </span>
                    </div>
                </section>

                {/* Column 2: Right panels */}
                <aside className="col-side">
                    <IndicatorsPanel indicators={currentAnalysis?.indicators15m} loading={loadingAnalysis} />
                    <AgentReasoningBox decision={currentDecision} loading={loadingDecision} />
                    <NewsFeed news={currentNews} loading={loadingNews} />
                </aside>
            </main>

            {showTelegram && <TelegramModal onClose={() => setShowTelegram(false)} />}
        </div>
    );
}
