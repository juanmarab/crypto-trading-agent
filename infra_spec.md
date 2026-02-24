AI Role: Act as a Cloud Infrastructure Architect and DBA.

Objective: Design an efficient relational and vector database schema using PostgreSQL.

Database Design:

Extension: Enable pgvector.

Table Market_News: Columns: Id, Asset (ENUM/String restricted to 'BTC', 'ETH', 'SOL', 'BNB'), Headline, Content, PublishedAt (Indexed for chronological sorting), and Embedding (vector type).

Table Technical_Snapshots: Stores the exact state of the RSI, MACD, etc., at the moment the AI made a decision, linked to the specific asset.

Table Agent_Decisions: A historical log of the raw LLM output to audit the AI's win-rate over time.

Table Users_Alerts: Stores Telegram Chat IDs and user preferences (e.g., "Only alert me for SOL and BNB").

Query Strategy: The vector similarity search (<=>) MUST include a strict WHERE clause filtering by the specific Asset being analyzed and ignoring any news older than 24 hours (PublishedAt), ensuring the LLM only reasons with fresh, asset-specific data.
