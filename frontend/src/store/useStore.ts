import * as signalR from '@microsoft/signalr';
import { create } from 'zustand';
import type { CryptoAsset, PriceTick, AgentDecision, TechnicalAnalysisDto, MarketNewsItem } from '../types';
import { fetchAnalysis, fetchLatestDecision, fetchNews } from '../api/client';

const HUB_URL = (import.meta.env.VITE_API_URL ?? 'http://localhost:5000') + '/hubs/price';

// ── Store shape ───────────────────────────────────────────────────────────────

interface AppState {
    activeAsset: CryptoAsset;
    setActiveAsset: (a: CryptoAsset) => void;

    prices: Partial<Record<CryptoAsset, PriceTick>>;
    hubStatus: 'connecting' | 'connected' | 'disconnected';

    analysis: Partial<Record<CryptoAsset, TechnicalAnalysisDto>>;
    decision: Partial<Record<CryptoAsset, AgentDecision>>;
    news: Partial<Record<CryptoAsset, MarketNewsItem[]>>;

    loadingAnalysis: boolean;
    loadingDecision: boolean;
    loadingNews: boolean;

    loadAssetData: (asset: CryptoAsset) => Promise<void>;
    startHub: () => void;
    stopHub: () => void;
}

let hubConnection: signalR.HubConnection | null = null;

export const useStore = create<AppState>((set, get) => ({
    activeAsset: 'BTC',
    setActiveAsset: async (asset) => {
        set({ activeAsset: asset });
        await get().loadAssetData(asset);
    },

    prices: {},
    hubStatus: 'disconnected',
    analysis: {},
    decision: {},
    news: {},
    loadingAnalysis: false,
    loadingDecision: false,
    loadingNews: false,

    loadAssetData: async (asset) => {
        set({ loadingAnalysis: true, loadingDecision: true, loadingNews: true });

        const [analysisResult, decisionResult, newsResult] = await Promise.allSettled([
            fetchAnalysis(asset),
            fetchLatestDecision(asset),
            fetchNews(asset),
        ]);

        set(state => ({
            analysis: { ...state.analysis, [asset]: analysisResult.status === 'fulfilled' ? analysisResult.value : undefined },
            decision: { ...state.decision, [asset]: decisionResult.status === 'fulfilled' ? (decisionResult.value ?? undefined) : undefined },
            news: { ...state.news, [asset]: newsResult.status === 'fulfilled' ? newsResult.value : [] },
            loadingAnalysis: false,
            loadingDecision: false,
            loadingNews: false,
        }));
    },

    startHub: () => {
        if (hubConnection) return;

        hubConnection = new signalR.HubConnectionBuilder()
            .withUrl(HUB_URL)
            .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
            .configureLogging(signalR.LogLevel.Warning)
            .build();

        hubConnection.on('PriceTick', (tick: PriceTick) => {
            set(state => ({
                prices: { ...state.prices, [tick.asset]: tick }
            }));
        });

        hubConnection.onreconnecting(() => set({ hubStatus: 'connecting' }));
        hubConnection.onreconnected(() => set({ hubStatus: 'connected' }));
        hubConnection.onclose(() => set({ hubStatus: 'disconnected' }));

        set({ hubStatus: 'connecting' });
        hubConnection
            .start()
            .then(() => set({ hubStatus: 'connected' }))
            .catch(() => set({ hubStatus: 'disconnected' }));
    },

    stopHub: () => {
        hubConnection?.stop();
        hubConnection = null;
        set({ hubStatus: 'disconnected' });
    },
}));
