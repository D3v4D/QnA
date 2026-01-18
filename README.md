# QnA App - Knowledge Base Q&A System

A Knowledge Base Question & Answer system using ASP.NET Core backend with AI-powered embeddings and Mistral LLM, paired with a React frontend.

**Clarification**: 

The backend architecture was designed and implemented by me. However, the frontend React application was made by Claude, based on my prompts.
    
The documentation was also written by AI, but I reviewed and edited it for accuracy.
    

## 🧠 Design Decisions & Reasoning

### What specifications were given?
- ASP.NET Core backend (with RESTful API)
- (React) frontend for user interaction
- AI-powered embeddings for knowledge retrieval
- Mistral LLM for answer generation
- Support for TXT, PDF, and HTML knowledge sources
- Chunking of documents for better context handling
- Caching of answers for performance

### Why Docker?
- **Consistency**: Same environment across development, testing, and production
- **Easy deployment**: Single command to start the entire stack
- **Isolation**: Backend and frontend run independently without conflicts

### Why Cosine Similarity?
- **Standard metric**: Industry-standard for comparing text embeddings
- **Normalized**: Works regardless of text length (0-1 scale)
- **Efficient**: Fast computation for real-time queries
- **Interpretable**: Higher score = more semantically similar

### Why Load Knowledge Base at Startup?
- **Predictable performance**: First user doesn't experience slow initial query
- **Immediate availability**: System is ready to answer questions from the start
- **Fail-fast**: Embedding errors are caught during startup, not during user queries
- **Better logging**: Clear visibility into what's loaded and any issues

### Why Restrict Mistral to Knowledge Base Only?
- **Accuracy**: Prevents strong hallucinations and made-up information
- **Transparency**: Users know answers come from verified sources
- **Compliance**: Important for regulated industries (healthcare, finance, legal)
- **Trust**: Clear when the system doesn't know vs. guessing

### Why ConcurrentDictionary for Cache?
- **Thread-safety**: ASP.NET handles multiple requests simultaneously
- **Lock-free**: Better performance than manual locking
- **Atomic operations**: `TryAdd`, `TryRemove` prevent race conditions

### Why Static Cache in Controller?
- **Shared state**: All controller instances share the same cache
- **Memory efficient**: One cache for the entire application
- **Singleton pattern**: Ensures consistency across requests
- **Simple implementation**: No need for external caching service (Redis) for this scale

### Why Singleton KnowledgeBaseService?
- **One knowledge base**: All embeddings loaded once and shared
- **Memory efficiency**: No duplicate embeddings in memory
- **Consistency**: All requests query the same indexed content
- **Performance**: Loading happens once at startup

### Why Only 3 Endpoints?
The application has exactly 3 endpoints - the minimum needed for full functionality:

1. **`POST /api/QnA/ask`** - Ask questions, get AI-generated answers from the knowledge base
2. **`POST /api/KnowledgeSource/upload-file`** - Upload PDF/TXT files to expand the knowledge base
3. **`POST /api/KnowledgeSource/upload-url`** - Download and index web pages (Wikipedia articles, documentation, etc.)

### Why Auto-Reload After Upload?
- **Immediate indexing**: Uploaded content becomes searchable instantly
- **Better UX**: Users can ask questions about new content right away
- **No manual steps**: System handles everything automatically
- **Consistency**: Always in sync with filesystem

## 🚀 Quick Start with Docker

### Prerequisites
- Docker and Docker Compose installed on your system
- Mistral AI API key (for answer generation)
- Optional: OpenAI or Cohere API key (for embeddings, or use the built-in OnPremise embedder)

### Initial Setup

1. **Configure the API keys in appsettings.json:**
   - Open `KnowledgeBaseService/appsettings.json`
   - Set your Mistral API key under `AI:LLM:Mistral:ApiKey`
   - (Optional) Set OpenAI or Cohere API key under `AI:Embedding` if using cloud embeddings

### Starting the Application

2. **Build and start all services:**
   ```bash
   docker-compose up --build
   ```

3. **Access the application:**
   - **Frontend**: http://localhost:3000
   - **Backend API**: http://localhost:5000
   - **Swagger UI**: http://localhost:5000/swagger

4. **Stop the application:**
   ```bash
   docker-compose down
   ```

### Running Without Docker

**Backend**: `cd Backend && dotnet run` (requires .NET 8 SDK)  
**Frontend**: `cd Frontend && npm install && npm run dev` (requires Node.js)

## 📁 Project Structure

```
QnAApp/
├── KnowledgeBaseService/       # ASP.NET Core backend
│   ├── Controllers/            # API controllers
│   ├── Services/               # Business logic & AI services
│   ├── Models/                 # Data models
│   └── KnowledgeSources/       # Knowledge base files (.txt, .pdf, .html)
├── Frontend/                   # React frontend
│   └── src/                    # React components
├── docker-compose.yml          # Docker orchestration
├── Dockerfile.backend          # Backend Docker image
└── Dockerfile.frontend         # Frontend Docker image
```

## 🔧 Configuration

### Backend Configuration

Edit `KnowledgeBaseService/appsettings.json`:

```json
{
  "AI": {
    "LLM": {
      "Mistral": {
        "Url": "https://api.mistral.ai",
        "ApiKey": "your-mistral-api-key"
      }
    },
    "Embedding": {
      "Provider": "OpenAI",  // or "OnPremise"
      "OpenAI": {
        "ApiKey": "your-openai-api-key"
      }
    }
  }
}
```

### Adding Knowledge Sources

Place your knowledge files in `KnowledgeBaseService/KnowledgeSources/`:
- **Text files**: `.txt`
- **PDF files**: `.pdf`
- **HTML files**: `.html` or `.htm`

The system will automatically detect and process them on startup.

## 🎯 Features

- **Multi-format Support**: Automatically extracts text from TXT, PDF, and HTML files
- **Smart Embeddings**: Uses either OpenAI, Cohere or on-premise hash-based embeddings (I couldn't tested OpenAI or Cohere due to lack of tokens)
- **AI-Powered Answers**: Leverages Mistral AI to generate structured responses (I haven't even implemeted OpenAI for the reason above)
- **Semantic Search**: Finds relevant context using cosine similarity
- **Simple UI**: Clean React interface for asking questions
- **Knowledge Upload**: Upload PDF/TXT files or download content from URLs directly through the UI
- **Dockerized**: Easy deployment with Docker Compose
- **Markdown Rendering**: Answers are beautifully formatted with markdown support

## 📊 Architecture

1. **Knowledge Base Loading**: Files are chunked into 500-character segments with 50-character overlap
2. **Embedding Generation**: Each chunk is converted to a vector representation
3. **Query Processing**: User questions are embedded and compared against all chunks
4. **Context Retrieval**: Top 3 most similar chunks are selected
5. **Answer Generation**: Mistral AI generates a structured answer based on the context

## 🛠️ Docker Commands

```bash
# Build images
docker-compose build

# Start services in background
docker-compose up -d

# View logs
docker-compose logs -f

# Stop services
docker-compose down

# Rebuild and restart
docker-compose up --build

# Remove all containers and volumes
docker-compose down -v
```

## 🔍 API Endpoints

### Question & Answer
- `POST /api/QnA/ask` - Submit a question and get an AI-generated answer
  ```json
  Request:
  {
    "question": "What is computer security?"
  }
  
  Response:
  {
    "answer": "Computer security is the protection of computer systems..."
  }
  ```

### Knowledge Source Management
- `POST /api/KnowledgeSource/upload-file` - Upload a PDF or TXT file
  - Content-Type: `multipart/form-data`
  - Form field: `file`
  - Max size: 100MB
  - Returns: Upload status and filename
  
- `POST /api/KnowledgeSource/upload-url` - Download content from a URL and add it to the knowledge base
  ```json
  Request:
  {
    "url": "https://en.wikipedia.org/wiki/Computer_security"
  }
  
  Response:
  {
    "success": true,
    "message": "URL content downloaded successfully",
    "fileName": "en.wikipedia.org-wiki-Computer_security-20260118-120000.html"
  }
  ```

**Note**: Both upload endpoints automatically:
1. Save the file to KnowledgeSources directory
2. Reload and re-index the knowledge base
3. Clear the answer cache to ensure fresh responses

## 🧪 Testing

Test the API directly:
```bash
curl -X POST http://localhost:5000/api/QnA/ask \
  -H "Content-Type: application/json" \
  -d '{"question":"What is phishing?"}'
```

## 📝 Notes

- First startup may take longer as the knowledge base loads and embeddings are generated
- The on-premise embedder is used by default (no API costs)
- Configure Mistral API key in appsettings.json for answer generation
- Frontend runs on port 3000, backend on port 5000# QnA
