import type { MarketNewsItem } from '../types';
import { Newspaper, ExternalLink, Clock } from 'lucide-react';

interface Props {
    news: MarketNewsItem[];
    loading?: boolean;
}

function SentimentDot({ score }: { score?: number }) {
    if (score == null) return <span className="dot neutral" />;
    if (score > 0.1) return <span className="dot bull" title={`Sentiment: +${score.toFixed(2)}`} />;
    if (score < -0.1) return <span className="dot bear" title={`Sentiment: ${score.toFixed(2)}`} />;
    return <span className="dot neutral" title="Neutral" />;
}

export default function NewsFeed({ news, loading }: Props) {
    return (
        <div className="glass-box panel-news">
            <div className="panel-header">
                <Newspaper size={16} className="icon-accent" />
                <h3>RAG News Feed</h3>
                <span className="badge">{news.length}</span>
            </div>

            {loading ? (
                <div className="news-skeleton">
                    {[1, 2, 3, 4].map(i => <div key={i} className="skeleton-line" />)}
                </div>
            ) : news.length === 0 ? (
                <p className="empty-news">No recent news ingested yet.</p>
            ) : (
                <ul className="news-list">
                    {news.map(item => (
                        <li key={item.id} className="news-item">
                            <SentimentDot score={item.sentimentScore} />
                            <div className="news-content">
                                <p className="news-headline">{item.headline}</p>
                                <div className="news-meta">
                                    <Clock size={11} />
                                    <span>{new Date(item.publishedAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}</span>
                                    {item.sourceUrl && (
                                        <a href={item.sourceUrl} target="_blank" rel="noreferrer" className="news-link">
                                            <ExternalLink size={11} />
                                        </a>
                                    )}
                                    {item.hasEmbedding && <span className="embed-badge">vec</span>}
                                </div>
                            </div>
                        </li>
                    ))}
                </ul>
            )}
        </div>
    );
}
