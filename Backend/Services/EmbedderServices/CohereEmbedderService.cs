using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace KnowledgeBaseService.Services;

public class CohereEmbedderService : IEmbedderService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public CohereEmbedderService(Microsoft.Extensions.Configuration.IConfiguration config)
    {
        _apiKey = config["AI:Embedding:Cohere:ApiKey"] ?? throw new System.InvalidOperationException("Cohere API key is not configured");
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new System.Uri("https://api.cohere.ai");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
    }

    public float[] Embed(string text)
    {
        var requestBody = new
        {
            texts = new[] { text },
            model = "embed-english-v3.0",
            input_type = "search_document"
        };

        var jsonContent = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var response = _httpClient.PostAsync("/v1/embed", content).Result;
        response.EnsureSuccessStatusCode();

        var responseBody = response.Content.ReadAsStringAsync().Result;
        var jsonResponse = JsonDocument.Parse(responseBody, new JsonDocumentOptions());

        var embeddingArray = jsonResponse.RootElement
            .GetProperty("embeddings")[0];

        var embedding = new System.Collections.Generic.List<float>();
        foreach (var element in embeddingArray.EnumerateArray())
        {
            embedding.Add((float)element.GetDouble());
        }

        return embedding.ToArray();
    }

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

