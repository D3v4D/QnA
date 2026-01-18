using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace KnowledgeBaseService.Services;

/// <summary>
/// Embedder service that uses OpenAI's API to generate text embeddings
/// </summary>
/// <remarks>
/// Uses the text-embedding-3-small model for efficient and cost-effective embeddings
/// </remarks>
public class OpenAiEmbedderService : IEmbedderService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    /// <summary>
    /// Initializes a new instance of the OpenAiEmbedderService
    /// </summary>
    /// <param name="config">Application configuration containing OpenAI API settings</param>
    public OpenAiEmbedderService(Microsoft.Extensions.Configuration.IConfiguration config)
    {
        _apiKey = config["AI:Embedding:OpenAI:ApiKey"] ?? throw new System.InvalidOperationException("OpenAI API key is not configured");
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new System.Uri("https://api.openai.com");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
    }

    /// <summary>
    /// Generates a vector embedding for the given text using OpenAI's embedding model
    /// </summary>
    /// <param name="text">The text to embed</param>
    /// <returns>A vector representation of the input text</returns>
    public float[] Embed(string text)
    {
        var requestBody = new
        {
            input = text,
            model = "text-embedding-3-small"
        };

        var jsonContent = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var response = _httpClient.PostAsync("/v1/embeddings", content).Result;
        response.EnsureSuccessStatusCode();

        var responseBody = response.Content.ReadAsStringAsync().Result;
        var jsonResponse = JsonDocument.Parse(responseBody, new JsonDocumentOptions());

        var embeddingArray = jsonResponse.RootElement
            .GetProperty("data")[0]
            .GetProperty("embedding");

        var embedding = new System.Collections.Generic.List<float>();
        foreach (var element in embeddingArray.EnumerateArray())
        {
            embedding.Add((float)element.GetDouble());
        }

        return embedding.ToArray();
    }

    /// <summary>
    /// Calculates the cosine similarity between two embedding vectors
    /// </summary>
    /// <param name="a">First embedding vector</param>
    /// <param name="b">Second embedding vector</param>
    /// <returns>Similarity score between 0 and 1, where 1 means identical</returns>
    public float CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length)
        {
            throw new System.ArgumentException("Vectors must have the same length");
        }

        float dotProduct = 0;
        float magnitudeA = 0;
        float magnitudeB = 0;

        for (int i = 0; i < a.Length; i++)
        {
            dotProduct += a[i] * b[i];
            magnitudeA += a[i] * a[i];
            magnitudeB += b[i] * b[i];
        }

        magnitudeA = System.MathF.Sqrt(magnitudeA);
        magnitudeB = System.MathF.Sqrt(magnitudeB);

        if (magnitudeA == 0 || magnitudeB == 0)
        {
            return 0;
        }

        return dotProduct / (magnitudeA * magnitudeB);
    }
}

