# Semantic Search Project

A modern .NET 10 + React + PostgreSQL (pgvector) application for semantic, keyword, and hybrid search of short stories.

## 🚀 Quick Start

1.  **Run Everything**: Start the entire stack with one command:
    ```bash
    docker compose up --build -d
    ```

2.  **Wait for Initialization**:
    The first run will take a few minutes as the API automatically:
    - Pulls the AI models (`nomic-embed-text` and `gemma3:1b`)
    - Migrates the PostgreSQL database
    - Summarizes and seeds the stories

3.  **Access**:
    - **Frontend**: [http://localhost:5173](http://localhost:5173)
    - **API Swagger**: [http://localhost:5041/swagger](http://localhost:5041/swagger)
    - **pgAdmin (DB Web UI)**: [http://localhost:5050](http://localhost:5050)

## 📊 Monitoring
To see the progress of the model pulling and story seeding:
```bash
docker compose logs -f api
```

## 🗄️ Database Management
You can manage the PostgreSQL database via **pgAdmin**:
- **Login**: `admin@admin.com` / `admin`
- **Add Server Connection**:
  - **Host**: `postgres` (internal Docker name)
  - **Port**: `5432`
  - **Maintenance DB**: `semantic_search`
  - **Username**: `user`
  - **Password**: `password`


## 🛠 Tech Stack

- **Backend**: ASP.NET Core 10 (Minimal APIs)
- **Database**: PostgreSQL with `pgvector` for vector storage and HNSW indexing.
- **AI/LLM**: Ollama (`nomic-embed-text` for embeddings, `gemma3:1b` for story summarization).
- **Frontend**: React 18 + TypeScript + Vite + Lucide React.
- **ORM**: Entity Framework Core with `Pgvector.EntityFrameworkCore`.

## 🔍 Search Capabilities

The API supports three search modes via the `/api/stories/search` endpoint:

1.  **Semantic**: Vector-based search using cosine distance. Finds meaning even without keyword matches.
2.  **Keyword**: Traditional PostgreSQL Full-Text Search (TSVector) for precise matches.
3.  **Hybrid**: Combines both (70% Semantic / 30% Keyword) for optimal results.

## ✨ Recent Improvements

- **Relevancy Scores**: Added a 0-100% match score to results based on normalized distances/ranks.
- **Short Summaries**: Seeder now generates concise 1-2 sentence summaries using LLM.
- **Clean API**: `Embedding` vectors are excluded from JSON responses (`[JsonIgnore]`) to save bandwidth.
- **Modern UI**: Created a clean React interface with search mode selection and story viewing modals.
- **Migration Fixes**: Resolved issues with `pgvector` index dimensions and migration namespaces.

## 📂 Project Structure

- `src/Api`: .NET 10 Web API.
- `src/web`: React TypeScript frontend.
- `src/Stories`: Source text files and metadata for seeding.
- `src/compose.yaml`: Docker configuration for local dev.
