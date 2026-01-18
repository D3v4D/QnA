namespace KnowledgeBaseService.Services;

public interface IEmbedderService
{
    /// <summary>
    /// Generates an embedding vector for the given text
    /// </summary>
    /// <param name="text">The text to embed</param>
    /// <returns>A float array representing the embedding vector</returns>
    float[] Embed(string text);
    
    /// <summary>
    /// Calculates the cosine similarity between two embedding vectors
    /// </summary>
    /// <param name="embedding1">First embedding vector</param>
    /// <param name="embedding2">Second embedding vector</param>
    /// <returns>Similarity score between 0 and 1</returns>
    float CosineSimilarity(float[] embedding1, float[] embedding2);
}