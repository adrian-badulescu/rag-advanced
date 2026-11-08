# 🏗️ Architecture & Call Flow

An end-to-end RAG (Retrieval-Augmented Generation) pipeline featuring semantic document ingestion, hybrid search (dense vector + FTS), local cross-encoder reranking, and LLM response generation.

---

## 📑 Table of Contents
- [1. Document Ingestion Flow (`/api/ingest`)](#1-document-ingestion-flow-apiingest)
- [2. Chat & Retrieval Flow (`/api/chat`)](#2-chat--retrieval-flow-apichat)

---

## 1. Document Ingestion Flow (`/api/ingest`)

This flow handles incoming raw documents, segments them into semantic chunks using NLP, generates vector embeddings, and stores them in PostgreSQL using `pgvector`.

### 🔄 Flow Diagram

```text
[HTTP Client] ──( POST /api/ingest )──> IngestEndpoint
                                              │
                                              ▼
                                   DataIngestionService
                                     ├── Catalyst NLP Pipeline
                                     ├── CreateSemanticChunksAsync
                                     │         │
                                     │         ▼
                                     │   EmbeddingService ──( HTTP )──> [Embedding API]
                                     │
                                     ▼
                               RagDbContext (EF Core)
                                     │
                                     ▼
                             [PostgreSQL + pgvector]
```

### 🧩 Components & Execution Sequence

#### 1. `IngestEndpoint.MapIngestEndpoint`
- **Endpoint:** `HTTP POST /api/ingest`
- **Caller:** HTTP Client (e.g., cURL, Frontend)
- **Description:** Receives the raw document request, validates payload fields, and delegates execution to the ingestion service.

#### 2. `DataIngestionService.IngestDocumentAsync`
- **Caller:** `IngestEndpoint`
- **Description:** Initializes the Catalyst NLP pipeline (`DiskStorage`, language tokenizer), processes raw text into linguistic sentences, and triggers semantic or standard chunking based on configuration.

#### 3. `DataIngestionService.CreateSemanticChunksAsync`
- **Caller:** `IngestDocumentAsync`
- **Description:** Computes cosine similarity between consecutive sentences using their embedding vectors; groups related sentences into coherent semantic chunks based on a configured similarity threshold.

#### 4. `EmbeddingService.GetEmbeddingsAsync` / `GetEmbeddingAsync`
- **Caller:** `CreateSemanticChunksAsync` & `IngestDocumentAsync`
- **Description:** Interacts with the external embedding model API via `HttpClient` to convert text fragments into high-dimensional numerical vectors.

#### 5. `RagDbContext` (EF Core)
- **Caller:** `IngestDocumentAsync`
- **Description:** Persists the generated chunks along with their vector representations into the PostgreSQL database table (`TextChunks`).

---

## 2. Chat & Retrieval Flow (`/api/chat`)

This flow processes user queries, performs hybrid search (dense vector + full-text search), reranks candidates locally using an ONNX cross-encoder model, and generates an AI response via Gemini.

### 🔄 Flow Diagram

```text
[HTTP Client] ──( POST /api/chat )──> ChatsEndpoint
                                            │
                                            ▼
                                       ChatService
                                            │
                                            ▼
                                  DataRetrievalService
                                    ├── EmbeddingService ──> [Embedding API]
                                    ├── HybridSearchRepository
                                    │         │
                                    │         ▼
                                    │    [PostgreSQL] (HNSW + FTS + RRF)
                                    │
                                    └── OnnxRerankerService (Local Inference)
                                            │
                                            ▼
                                      [Gemini LLM API]
```

### 🧩 Components & Execution Sequence

#### 1. `ChatsEndpoint.MapChatEndpoint`
- **Endpoint:** `HTTP POST /api/chat`
- **Caller:** HTTP Client
- **Description:** Validates incoming chat message length and structure, applying rate-limiting policies before calling the chat service.

#### 2. `ChatService.GetAiResponseAsync`
- **Caller:** `ChatsEndpoint`
- **Description:** Orchestrates context retrieval, constructs the prompt template using retrieved snippets, and communicates with the LLM API to generate the final response.

#### 3. `DataRetrievalService.GetRelevantContextAsync`
- **Caller:** `ChatService`
- **Description:** Converts the user query into an embedding vector and coordinates candidate retrieval alongside local reranking.

#### 4. `EmbeddingService.GetEmbeddingAsync`
- **Caller:** `DataRetrievalService`
- **Description:** Generates the vector representation for the user's natural language query.

#### 5. `HybridSearchRepository.GetHybridResultsAsync`
- **Caller:** `DataRetrievalService`
- **Description:** Executes an advanced PostgreSQL query combining **HNSW vector similarity search** (`vector_search`) and **PostgreSQL Full-Text Search** (`text_search`), merging results using **Reciprocal Rank Fusion (RRF)**.

#### 6. `OnnxRerankerService.Rank`
- **Caller:** `DataRetrievalService`
- **Description:** Tokenizes query-candidate pairs and runs local inference using an ONNX-optimized cross-encoder model to compute accurate relevance scores, sorting and returning top-tier candidates.
