namespace KnowledgeBaseService.Models;

/// <summary>
/// Represents a text chunk with its embedding vector and source information
/// </summary>
/// <remarks>
/// This class is used to store chunked text from knowledge sources along with their
/// vector embeddings for similarity-based retrieval
/// </remarks>
public class EmbeddedChunk
{
    /// <summary>
    /// Gets or sets the text content of the chunk
    /// </summary>
    public string Text { get; set; }
    
    /// <summary>
    /// Gets or sets the embedding vector representation of the text
    /// </summary>
    public float[] Embedding { get; set; }
    
    /// <summary>
    /// Gets or sets the name of the source this chunk originated from
    /// </summary>
    public string SourceName { get; set; }

    /// <summary>
    /// Initializes a new instance of the EmbeddedChunk class
    /// </summary>
    /// <param name="text">The text content of the chunk</param>
    /// <param name="embedding">The embedding vector for the text</param>
    /// <param name="sourceName">The name of the source document</param>
    public EmbeddedChunk(string text, float[] embedding, string sourceName)
    {
        Text = text;
        Embedding = embedding;
        SourceName = sourceName;
    }
}

