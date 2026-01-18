using System.Text.RegularExpressions;

namespace KnowledgeBaseService.Services;

/// <summary>
/// On-premise embedding service that generates embeddings using a hash-based algorithm without requiring external APIs
/// </summary>
/// <remarks>
/// This embedder uses a simple but effective hashing strategy to distribute words across embedding dimensions.
/// It filters out common stop words and normalizes the resulting vectors for cosine similarity calculations.
/// </remarks>
public class OnPremiseHashEmbedderService : IEmbedderService
{
    private readonly int _embeddingDimension;
    private readonly HashSet<string> _stopWords;
    
    /// <summary>
    /// Initializes a new instance of the OnPremiseEmbedderService
    /// </summary>
    /// <param name="embeddingDimension">The dimension of the embedding vectors (default: 300)</param>
    public OnPremiseHashEmbedderService(int embeddingDimension = 300)
    {
        _embeddingDimension = embeddingDimension;
        _stopWords = new HashSet<string>
        {
            "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for",
            "of", "with", "by", "from", "as", "is", "was", "are", "were", "be",
            "been", "being", "have", "has", "had", "do", "does", "did", "will",
            "would", "should", "could", "may", "might", "must", "can", "this",
            "that", "these", "those", "i", "you", "he", "she", "it", "we", "they"
        };
    }
    
    /// <summary>
    /// Generates a simple embedding based on word hashing and normalization
    /// </summary>
    /// <param name="text">The text to embed</param>
    /// <returns>A normalized embedding vector of the specified dimension</returns>
    /// <remarks>
    /// Uses multiple hash functions to distribute each word across the embedding space,
    /// filters stop words, and normalizes the result for cosine similarity
    /// </remarks>
    public float[] Embed(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new float[_embeddingDimension];
        }
        
        var embedding = new float[_embeddingDimension];
        var words = Tokenize(text);
        
        // Simple hash-based embedding: each word contributes to multiple dimensions
        foreach (var word in words)
        {
            if (_stopWords.Contains(word.ToLower()))
                continue;
                
            var hash = GetStableHash(word);
            
            // Use multiple hash functions to distribute word across dimensions
            for (int i = 0; i < 3; i++)
            {
                var index = Math.Abs((hash + i * 31) % _embeddingDimension);
                var value = 1.0f / (1.0f + i); // Decay for secondary hashes
                embedding[index] += value;
            }
        }
        
        // Normalize the embedding vector
        return Normalize(embedding);
    }
    
    /// <summary>
    /// Calculates the cosine similarity between two embedding vectors
    /// </summary>
    /// <param name="embedding1">First embedding vector</param>
    /// <param name="embedding2">Second embedding vector</param>
    /// <returns>Similarity score between 0 and 1, where 1 indicates identical direction</returns>
    /// <exception cref="ArgumentException">Thrown when embeddings have different dimensions</exception>
    public float CosineSimilarity(float[] embedding1, float[] embedding2)
    {
        if (embedding1.Length != embedding2.Length)
        {
            throw new ArgumentException("Embeddings must have the same dimension");
        }
        
        float dotProduct = 0;
        float magnitude1 = 0;
        float magnitude2 = 0;
        
        for (int i = 0; i < embedding1.Length; i++)
        {
            dotProduct += embedding1[i] * embedding2[i];
            magnitude1 += embedding1[i] * embedding1[i];
            magnitude2 += embedding2[i] * embedding2[i];
        }
        
        if (magnitude1 == 0 || magnitude2 == 0)
            return 0;
        
        return dotProduct / (float)(Math.Sqrt(magnitude1) * Math.Sqrt(magnitude2));
    }
    
    /// <summary>
    /// Tokenizes text into individual words, filtering out whitespace and single-character tokens
    /// </summary>
    /// <param name="text">The text to tokenize</param>
    /// <returns>List of lowercase word tokens</returns>
    private List<string> Tokenize(string text)
    {
        // Convert to lowercase and extract words
        var words = Regex.Split(text.ToLower(), @"\W+")
            .Where(w => !string.IsNullOrWhiteSpace(w) && w.Length > 1)
            .ToList();
        
        return words;
    }
    
    /// <summary>
    /// Generates a stable hash code for a word to ensure consistent embedding placement
    /// </summary>
    /// <param name="word">The word to hash</param>
    /// <returns>A non-negative hash code</returns>
    private int GetStableHash(string word)
    {
        // Simple stable hash function
        int hash = 0;
        foreach (char c in word)
        {
            hash = (hash * 31 + c) & 0x7FFFFFFF;
        }
        return hash;
    }
    
    /// <summary>
    /// Normalizes a vector to unit length for cosine similarity calculations
    /// </summary>
    /// <param name="vector">The vector to normalize</param>
    /// <returns>A normalized vector with magnitude 1 (or the original if magnitude is 0)</returns>
    private float[] Normalize(float[] vector)
    {
        float magnitude = 0;
        for (int i = 0; i < vector.Length; i++)
        {
            magnitude += vector[i] * vector[i];
        }
        
        magnitude = (float)Math.Sqrt(magnitude);
        
        if (magnitude == 0)
            return vector;
        
        var normalized = new float[vector.Length];
        for (int i = 0; i < vector.Length; i++)
        {
            normalized[i] = vector[i] / magnitude;
        }
        
        return normalized;
    }
}