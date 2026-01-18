using System.Text;
using KnowledgeBaseService.Models;
using System.Text.RegularExpressions;

namespace KnowledgeBaseService.Services;

/// <summary>
/// Main service for managing the knowledge base, including loading sources, creating embeddings, and retrieving relevant content
/// </summary>
public class KnowledgeBaseService
{
    private readonly List<IKnowledgeSource> _knowledgeSources;
    private readonly IEmbedderService _embedderService;
    private readonly List<EmbeddedChunk> _embeddedChunks = new();
    private readonly string _knowledgeSourcesPath = Path.Combine(AppContext.BaseDirectory, "KnowledgeSources");
    private const int ChunkSize = 500; // Characters per chunk
    private const int ChunkOverlap = 50; // Overlap between chunks
    
    /// <summary>
    /// Initializes a new instance of the KnowledgeBaseService
    /// </summary>
    /// <param name="embedderService">The embedding service to use for generating text embeddings</param>
    public KnowledgeBaseService(IEmbedderService embedderService)
    {
        _embedderService = embedderService;
        _knowledgeSources = new List<IKnowledgeSource>();
        InitializeKnowledgeSources();
    }
    
    /// <summary>
    /// Scans the KnowledgeSources directory and initializes knowledge sources for supported file types (txt, pdf, html)
    /// </summary>
    private void InitializeKnowledgeSources()
    {
        // Clear existing sources to avoid duplicates when rescanning
        _knowledgeSources.Clear();
        
        if (!Directory.Exists(_knowledgeSourcesPath))
        {
            Console.WriteLine($"Warning: KnowledgeSources directory not found at {_knowledgeSourcesPath}");
            return;
        }

        var files = Directory.GetFiles(_knowledgeSourcesPath);
        
        foreach (var filePath in files)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            
            IKnowledgeSource? source = extension switch
            {
                ".txt" => new TxtKnowledgeSource(filePath),
                ".pdf" => new PdfKnowledgeSource(filePath),
                ".html" or ".htm" => new HtmlKnowledgeSource(filePath),
                _ => null
            };
            
            if (source != null)
            {
                _knowledgeSources.Add(source);
                Console.WriteLine($"Added knowledge source: {Path.GetFileName(filePath)}");
            }
            else
            {
                Console.WriteLine($"Skipped unsupported file type: {Path.GetFileName(filePath)}");
            }
        }
    }
    
    /// <summary>
    /// Loads the knowledge base and creates embeddings for all chunks
    /// </summary>
    /// <remarks>
    /// This method processes all knowledge sources by:
    /// 1. Splitting content into overlapping chunks
    /// 2. Generating embeddings for each chunk using the configured embedder
    /// 3. Storing the embedded chunks for similarity-based retrieval
    /// Errors in individual chunks are logged but don't stop the overall process
    /// </remarks>
    public void LoadKnowledgeBase()
    {
        _embeddedChunks.Clear();
        
        // Rescan directory for new files (in case files were added after initialization)
        InitializeKnowledgeSources();
        
        foreach (var source in _knowledgeSources)
        {
            try
            {
                var content = source.GetContent();
                var sourceName = source.GetType().Name;
                
                // Split content into chunks
                var chunks = ChunkText(content);
                
                Console.WriteLine($"Processing {chunks.Count} chunks from {sourceName}...");
                
                // Create embeddings for each chunk
                foreach (var chunk in chunks)
                {
                    try
                    {
                        var embedding = _embedderService.Embed(chunk);
                        var embeddedChunk = new EmbeddedChunk(chunk, embedding, sourceName);
                        _embeddedChunks.Add(embeddedChunk);
                    }
                    catch (Exception embedEx)
                    {
                        Console.WriteLine($"Error embedding chunk from {sourceName}: {embedEx.Message}");
                        // Continue processing other chunks
                    }
                }
                
                Console.WriteLine($"Successfully processed {chunks.Count} chunks from {sourceName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading knowledge source: {ex.Message}");
            }
        }
        
        Console.WriteLine($"Knowledge base loaded with {_embeddedChunks.Count} embedded chunks from {_knowledgeSources.Count} sources.");
    }
    
    /// <summary>
    /// Splits text into overlapping chunks for better context preservation
    /// </summary>
    /// <param name="text">The text to split into chunks</param>
    /// <returns>List of text chunks with overlapping boundaries</returns>
    /// <remarks>
    /// Uses sentence boundaries to avoid splitting mid-sentence.
    /// Chunks have a maximum size of 500 characters with 50 characters of overlap to maintain context continuity.
    /// </remarks>
    private static List<string> ChunkText(string text)
    {
        var chunks = new List<string>();
        
        if (string.IsNullOrWhiteSpace(text))
            return chunks;
        
        // Split by sentences first for better chunk boundaries
        var sentences = Regex.Split(text, @"(?<=[.!?])\s+");
        
        var  currentChunk = "";
        
        foreach (var sentence in sentences)
        {
            if (currentChunk.Length + sentence.Length <= ChunkSize)
            {
                currentChunk += sentence + " ";
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(currentChunk))
                {
                    chunks.Add(currentChunk.Trim());
                }
                
                // Start new chunk with overlap from previous
                if (currentChunk.Length > ChunkOverlap)
                {
                    var overlapStart = currentChunk.Length - ChunkOverlap;
                    currentChunk = currentChunk.Substring(overlapStart) + sentence + " ";
                }
                else
                {
                    currentChunk = sentence + " ";
                }
            }
        }
        
        // Add the last chunk
        if (!string.IsNullOrWhiteSpace(currentChunk))
        {
            chunks.Add(currentChunk.Trim());
        }
        
        return chunks;
    }
    
    /// <summary>
    /// Retrieves the most relevant text chunks based on the query using cosine similarity
    /// </summary>
    /// <param name="query">The search query to find relevant chunks</param>
    /// <param name="topK">Number of top results to return (default: 3)</param>
    /// <returns>List of the most relevant text chunks ordered by similarity score</returns>
    /// <remarks>
    /// Uses the embedder service to generate a query embedding, then calculates cosine similarity
    /// against all stored chunks and returns the top K matches
    /// </remarks>
    public List<string> GetAnswers(string query, int topK = 3)
    {
        if (_embeddedChunks.Count == 0)
        {
            Console.WriteLine("Warning: Knowledge base is empty. Call LoadKnowledgeBase() first.");
            return new List<string>();
        }
        
        // Create embedding for the query
        var queryEmbedding = _embedderService.Embed(query);
        
        // Calculate similarities and rank chunks
        var rankedChunks = _embeddedChunks
            .Select(chunk => new
            {
                Chunk = chunk,
                Similarity = _embedderService.CosineSimilarity(queryEmbedding, chunk.Embedding)
            })
            .OrderByDescending(x => x.Similarity)
            .Take(topK)
            .ToList();
        
        Console.WriteLine($"Top {topK} results for query: '{query}'");
        foreach (var result in rankedChunks)
        {
            Console.WriteLine($"  - Similarity: {result.Similarity:F4} | Source: {result.Chunk.SourceName}");
        }
        
        return rankedChunks.Select(x => x.Chunk.Text).ToList();
    }
    
    /// <summary>
    /// Returns all embedded chunks for debugging and inspection purposes
    /// </summary>
    /// <returns>List of all embedded chunks currently in the knowledge base</returns>
    public List<EmbeddedChunk> GetEmbeddedChunks()
    {
        return _embeddedChunks;
    }
    
    /// <summary>
    /// Manually adds an embedded chunk to the knowledge base
    /// </summary>
    /// <param name="chunk">The embedded chunk to add</param>
    public void AddEmbeddedChunk(EmbeddedChunk chunk)
    {
        _embeddedChunks.Add(chunk);
    }
    
    /// <summary>
    /// Adds a knowledge source to the collection of sources
    /// </summary>
    /// <param name="source">The knowledge source to add</param>
    public void AddKnowledgeSource(IKnowledgeSource source)
    {
        _knowledgeSources.Add(source);
    }
    
    /// <summary>
    /// Gets the total number of knowledge sources currently loaded
    /// </summary>
    /// <returns>The count of knowledge sources</returns>
    public int GetSourceCount()
    {
        return _knowledgeSources.Count;
    }
    
    /// <summary>
    /// Gets the total number of embedded chunks in the knowledge base
    /// </summary>
    /// <returns>The count of embedded chunks</returns>
    public int GetChunkCount()
    {
        return _embeddedChunks.Count;
    }
}