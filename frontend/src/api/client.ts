import axios from 'axios';
import type { CryptoAsset, TechnicalAnalysisDto, AgentDecision, MarketNewsItem, KlineData } from '../types';

const BASE = import.meta.env.VITE_API_URL ?? 'http://localhost:5000';

const api = axios.create({ baseURL: BASE, timeout: 15000 });

export const fetchAnalysis = (asset: CryptoAsset): Promise<TechnicalAnalysisDto> =>
    api.get(`/api/technicalanalysis/analysis/${asset}`).then(r => r.data);

export const fetchKlines = (asset: CryptoAsset, interval = '15m', limit = 200): Promise<KlineData[]> =>
    api.get(`/api/technicalanalysis/klines/${asset}`, { params: { interval, limit } }).then(r => r.data);

export const fetchLatestDecision = (asset: CryptoAsset): Promise<AgentDecision | null> =>
    api.get(`/api/decisions/latest/${asset}`).then(r => r.data).catch(() => null);

export const fetchDecisions = (asset: CryptoAsset, limit = 10): Promise<AgentDecision[]> =>
    api.get(`/api/decisions/${asset}`, { params: { limit } }).then(r => r.data).catch(() => []);

export const fetchNews = (asset: CryptoAsset, hours = 24, limit = 15): Promise<MarketNewsItem[]> =>
    api.get(`/api/news/${asset}`, { params: { hours, limit } }).then(r => r.data).catch(() => []);

export const registerAlert = (chatId: string, threshold: number, assets: string[]) =>
    api.post('/api/alerts', { telegramChatId: chatId, alertOnlyAssets: assets, minConfidenceThreshold: threshold });

export const deleteAlert = (chatId: string) =>
    api.delete(`/api/alerts/${chatId}`);

export const fetchHealth = () =>
    api.get('/api/health').then(r => r.data).catch(() => ({ status: 'error' }));
