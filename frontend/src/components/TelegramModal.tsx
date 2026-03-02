import { useState } from 'react';
import { Bell, X, Loader2, CheckCircle } from 'lucide-react';
import { registerAlert } from '../api/client';

interface Props { onClose: () => void; }

const ALL_ASSETS = ['BTC', 'ETH', 'SOL', 'BNB'];

export default function TelegramModal({ onClose }: Props) {
    const [chatId, setChatId] = useState('');
    const [threshold, setThreshold] = useState(0.7);
    const [assets, setAssets] = useState<string[]>(ALL_ASSETS);
    const [status, setStatus] = useState<'idle' | 'loading' | 'success' | 'error'>('idle');
    const [errorMsg, setErrorMsg] = useState('');

    const toggleAsset = (a: string) =>
        setAssets(prev => prev.includes(a) ? prev.filter(x => x !== a) : [...prev, a]);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!chatId.trim()) return;
        setStatus('loading');
        setErrorMsg('');
        try {
            await registerAlert(chatId.trim(), threshold, assets);
            setStatus('success');
        } catch (err: any) {
            setStatus('error');
            setErrorMsg(err?.response?.data?.message ?? 'Failed to register alert.');
        }
    };

    return (
        <div className="modal-overlay" onClick={onClose}>
            <div className="modal-box" onClick={e => e.stopPropagation()}>
                <div className="modal-header">
                    <Bell size={18} className="icon-accent" />
                    <h3>Telegram Alerts</h3>
                    <button className="icon-btn" onClick={onClose}><X size={18} /></button>
                </div>

                {status === 'success' ? (
                    <div className="modal-success">
                        <CheckCircle size={36} className="success-icon" />
                        <p>Alert registered! You'll receive signals at chat ID <strong>{chatId}</strong>.</p>
                        <button className="btn-primary" onClick={onClose}>Done</button>
                    </div>
                ) : (
                    <form onSubmit={handleSubmit} className="modal-form">
                        <label className="form-label">
                            Telegram Chat ID
                            <input
                                className="form-input"
                                type="text"
                                placeholder="e.g. 123456789"
                                value={chatId}
                                onChange={e => setChatId(e.target.value)}
                                required
                            />
                            <span className="form-hint">Get your ID from @userinfobot on Telegram</span>
                        </label>

                        <label className="form-label">
                            Min. Confidence Threshold
                            <div className="slider-row">
                                <input
                                    type="range" min={0.3} max={1} step={0.05}
                                    value={threshold}
                                    onChange={e => setThreshold(parseFloat(e.target.value))}
                                />
                                <span className="slider-value">{Math.round(threshold * 100)}%</span>
                            </div>
                        </label>

                        <div className="form-label">
                            Monitor Assets
                            <div className="asset-pills">
                                {ALL_ASSETS.map(a => (
                                    <button
                                        key={a} type="button"
                                        className={`asset-pill ${assets.includes(a) ? 'active' : ''}`}
                                        onClick={() => toggleAsset(a)}
                                    >
                                        {a}
                                    </button>
                                ))}
                            </div>
                        </div>

                        {errorMsg && <p className="form-error">{errorMsg}</p>}

                        <button
                            className="btn-primary"
                            type="submit"
                            disabled={status === 'loading' || assets.length === 0}
                        >
                            {status === 'loading'
                                ? <><Loader2 size={16} className="spin" /> Registering…</>
                                : 'Register Alert'}
                        </button>
                    </form>
                )}
            </div>
        </div>
    );
}
