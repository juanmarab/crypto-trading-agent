AI Role: Act as a Senior Frontend Developer expert in React.

Objective: Develop a clean, responsive Single Page Application (SPA) using React (Vite or Next.js) to serve as the Trading Agent's Dashboard.

Functional Requirements:

Asset Selector: A prominent navigation component allowing the user to switch the active dashboard context between BTC, ETH, SOL, and BNB.

Advanced Charting Panel: Integrate lightweight-charts (by TradingView) to render the OHLCV candlestick chart for the selected asset. The chart must overlay EMAs and Bollinger Bands, with separate lower panes for MACD and RSI.

State Management: Use Zustand or Redux Toolkit to handle the real-time data streams and historical data fetched from the backend.

Semantic RAG Feed: A side panel displaying recently ingested news headlines specific to the selected asset, featuring a visual indicator (green/red) of the isolated sentiment for each news item.

Agent Reasoning Box (Transparency): The core component displaying the LLM's "glass box" logic. It must break down:

Technical Verdict (Indicator summary).

Fundamental Verdict (Citations of relevant news).

Suggested Action (Long/Short/Hold) and Suggested Leverage.

Telegram Configuration: A settings modal for the user to input their Chat ID and define custom alert thresholds.
