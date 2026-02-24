AI Role: Act as a Senior Software Architect and Quantitative Financial Analyst.

Project Description: Development of an "AI Crypto Trading Agent" (SaaS). The system acts as a hybrid algorithmic trading assistant that automates market analysis using two concurrent engines: Technical Analysis (Mathematical) and Fundamental Analysis (News via RAG - Retrieval-Augmented Generation).

Scope Limitation: To ensure high data quality and system performance, the agent will strictly monitor and analyze only four major cryptocurrency pairs: BTC/USDT, ETH/USDT, SOL/USDT, and BNB/USDT.

The Problem: Manual trading in highly volatile pairs requires simultaneously evaluating complex mathematical indicators and processing global financial news. This manual process introduces fatal latency in decision-making.

The Solution: An automated orchestrator. The backend will calculate a full battery of indicators (RSI, MACD, Bollinger Bands, EMA crossovers, and ATR for volatility). Simultaneously, it will ingest the latest financial news via RAG. An LLM will evaluate the convergence of these technical and fundamental data points to issue a trading recommendation (Long/Short/Hold) and suggest precise risk management parameters (e.g., leverage levels based on ATR and news sentiment volatility).

Delivery: A React SPA acting as the "Command Center" to visualize the AI's reasoning, supported by real-time Push alerts via Telegram.
