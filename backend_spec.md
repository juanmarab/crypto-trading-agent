AI Role: Act as a Senior Backend Developer expert in C# and .NET 8.

Objective: Build the logic engine, data ingestion pipelines, and AI orchestrator.

Module 1: Technical Analysis (Logical Brain)
Consumes the Binance API. Must strictly filter data for BTCUSDT, ETHUSDT, SOLUSDT, and BNBUSDT.

Calculations: EMA (20, 50, 200), RSI (14 periods), MACD, Bollinger Bands, and ATR.

Frequencies: >   * Live Prices: WebSocket connection to Binance for real-time frontend updates.

Indicator Calculation: REST API polling (Klines/OHLCV) every 5 minutes and 15 minutes to evaluate multi-timeframe convergence.

Module 2: RAG Ingestion (Semantic Brain)
A Background Worker Service consuming the CryptoPanic API (or similar).

Execution: A Cron Job running every 15 minutes.

Logic: Fetches news specifically tagged for Bitcoin, Ethereum, Solana, and BNB. Extracts text, calls an embedding model (e.g., text-embedding-3-small), and stores the vectors in the database.

Module 3: AI Orchestrator & Telegram Integration
Every 15 minutes (synchronized with candle closes and news updates), the service packages the current technical indicators and semantic context retrieved from the vector DB. It sends a structured prompt to the LLM (Gemini/Groq). If the LLM identifies a high-probability setup exceeding risk thresholds, it triggers an alert payload to the Telegram API.
