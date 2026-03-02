import type { KlineData, IndicatorSet } from '../types';
import { useEffect, useRef } from 'react';
import { createChart, ColorType, CrosshairMode, CandlestickSeries, LineSeries } from 'lightweight-charts';

interface Props {
    klines: KlineData[];
    indicators?: IndicatorSet;
}

// eslint-disable-next-line @typescript-eslint/no-explicit-any
type AnySeriesApi = any;
type ChartApi = ReturnType<typeof createChart>;

interface ChartRefs {
    chart: ChartApi;
    candle: AnySeriesApi;
    ema20: AnySeriesApi;
    ema50: AnySeriesApi;
    ema200: AnySeriesApi;
    bbUpper: AnySeriesApi;
    bbLower: AnySeriesApi;
}

export default function CandlestickChart({ klines, indicators }: Props) {
    const containerRef = useRef<HTMLDivElement>(null);
    const inited = useRef(false);
    const refs = useRef<ChartRefs | null>(null);

    // Build chart once
    useEffect(() => {
        if (!containerRef.current || inited.current) return;
        inited.current = true;

        const chart = createChart(containerRef.current, {
            layout: {
                background: { type: ColorType.Solid, color: '#0f111a' },
                textColor: '#9ca3af',
            },
            grid: {
                vertLines: { color: '#1e2133' },
                horzLines: { color: '#1e2133' },
            },
            crosshair: { mode: CrosshairMode.Normal },
            rightPriceScale: { borderColor: '#1e2133' },
            timeScale: { borderColor: '#1e2133', timeVisible: true },
            width: containerRef.current.clientWidth,
            height: containerRef.current.clientHeight,
        });

        refs.current = {
            chart,
            candle: chart.addSeries(CandlestickSeries, { upColor: '#22c55e', downColor: '#ef4444', borderUpColor: '#22c55e', borderDownColor: '#ef4444', wickUpColor: '#22c55e', wickDownColor: '#ef4444' }),
            ema20: chart.addSeries(LineSeries, { color: '#facc15', lineWidth: 1, priceLineVisible: false, lastValueVisible: false }),
            ema50: chart.addSeries(LineSeries, { color: '#f97316', lineWidth: 1, priceLineVisible: false, lastValueVisible: false }),
            ema200: chart.addSeries(LineSeries, { color: '#a855f7', lineWidth: 1, priceLineVisible: false, lastValueVisible: false }),
            bbUpper: chart.addSeries(LineSeries, { color: '#3b82f666', lineWidth: 1, priceLineVisible: false, lastValueVisible: false, lineStyle: 2 }),
            bbLower: chart.addSeries(LineSeries, { color: '#3b82f666', lineWidth: 1, priceLineVisible: false, lastValueVisible: false, lineStyle: 2 }),
        };

        const ro = new ResizeObserver(() => {
            if (containerRef.current) chart.applyOptions({ width: containerRef.current.clientWidth });
        });
        ro.observe(containerRef.current);

        return () => {
            ro.disconnect();
            chart.remove();
            refs.current = null;
            inited.current = false;
        };
    }, []);

    // Update data on klines/indicator change
    useEffect(() => {
        const r = refs.current;
        if (!r || klines.length === 0) return;

        // lightweight-charts v5 expects UTCTimestamp (seconds)
        const toTs = (s: string) => Math.floor(new Date(s).getTime() / 1000);

        r.candle.setData(klines.map(k => ({
            time: toTs(k.openTime), open: k.open, high: k.high, low: k.low, close: k.close,
        })));

        const lastT = toTs(klines[klines.length - 1].openTime);
        r.ema20.setData(indicators?.ema20 != null ? [{ time: lastT, value: indicators.ema20 }] : []);
        r.ema50.setData(indicators?.ema50 != null ? [{ time: lastT, value: indicators.ema50 }] : []);
        r.ema200.setData(indicators?.ema200 != null ? [{ time: lastT, value: indicators.ema200 }] : []);
        r.bbUpper.setData(indicators?.bbUpper != null ? [{ time: lastT, value: indicators.bbUpper }] : []);
        r.bbLower.setData(indicators?.bbLower != null ? [{ time: lastT, value: indicators.bbLower }] : []);

        r.chart.timeScale().fitContent();
    }, [klines, indicators]);

    return <div ref={containerRef} className="chart-container" />;
}
