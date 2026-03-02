import type { CryptoAsset } from '../types';
import { useStore } from '../store/useStore';

const ASSETS: CryptoAsset[] = ['BTC', 'ETH', 'SOL', 'BNB'];

const ASSET_ICONS: Record<CryptoAsset, string> = {
    BTC: '₿', ETH: 'Ξ', SOL: '◎', BNB: '⬡',
};

const ASSET_COLORS: Record<CryptoAsset, string> = {
    BTC: '#f7931a', ETH: '#627eea', SOL: '#9945ff', BNB: '#f3ba2f',
};

export default function AssetSelector() {
    const { activeAsset, setActiveAsset, prices } = useStore();

    return (
        <div className="asset-selector">
            {ASSETS.map(asset => {
                const price = prices[asset];
                const isActive = activeAsset === asset;
                const change = price?.priceChangePercent24h ?? 0;
                const positive = change >= 0;

                return (
                    <button
                        key={asset}
                        className={`asset-btn ${isActive ? 'active' : ''}`}
                        style={{ '--accent': ASSET_COLORS[asset] } as React.CSSProperties}
                        onClick={() => setActiveAsset(asset)}
                    >
                        <span className="asset-icon" style={{ color: ASSET_COLORS[asset] }}>
                            {ASSET_ICONS[asset]}
                        </span>
                        <div className="asset-info">
                            <span className="asset-name">{asset}</span>
                            {price ? (
                                <>
                                    <span className="asset-price">
                                        ${price.price.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                                    </span>
                                    <span className={`asset-change ${positive ? 'pos' : 'neg'}`}>
                                        {positive ? '+' : ''}{change.toFixed(2)}%
                                    </span>
                                </>
                            ) : (
                                <span className="asset-price muted">Loading…</span>
                            )}
                        </div>
                    </button>
                );
            })}
        </div>
    );
}
