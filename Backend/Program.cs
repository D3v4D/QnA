var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to allow larger file uploads
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 104857600; // 100 MB
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();

// Register HttpClient for services
builder.Services.AddHttpClient<KnowledgeBaseService.Services.IAnswerService, KnowledgeBaseService.Services.MistralAnswerService>();
builder.Services.AddHttpClient(); // For KnowledgeSourceController

// Register the appropriate embedder service based on configuration
var embeddingProvider = builder.Configuration["AI:Embedding:Provider"];

switch (embeddingProvider)
{
    case "OpenAI":
        var openAiApiKey = builder.Configuration["AI:Embedding:OpenAI:ApiKey"];
        if (!string.IsNullOrEmpty(openAiApiKey))
        {
            Console.WriteLine("Using OpenAI Embedder Service");
            builder.Services.AddSingleton<KnowledgeBaseService.Services.IEmbedderService, KnowledgeBaseService.Services.OpenAiEmbedderService>();
        }
        else
        {
            Console.WriteLine("OpenAI selected but API key not configured, falling back to OnPremise Hash Embedder Service");
            builder.Services.AddSingleton<KnowledgeBaseService.Services.IEmbedderService, KnowledgeBaseService.Services.OnPremiseHashEmbedderService>();
        }
        break;
    
    case "Cohere":
        var cohereApiKey = builder.Configuration["AI:Embedding:Cohere:ApiKey"];
        if (!string.IsNullOrEmpty(cohereApiKey))
        {
            Console.WriteLine("Using Cohere Embedder Service");
            builder.Services.AddSingleton<KnowledgeBaseService.Services.IEmbedderService, KnowledgeBaseService.Services.CohereEmbedderService>();
        }
        else
        {
            Console.WriteLine("Cohere selected but API key not configured, falling back to OnPremise Hash Embedder Service");
            builder.Services.AddSingleton<KnowledgeBaseService.Services.IEmbedderService, KnowledgeBaseService.Services.OnPremiseHashEmbedderService>();
        }
        break;
    
    default:
        Console.WriteLine("Using OnPremise Hash Embedder Service (no external provider configured)");
        builder.Services.AddSingleton<KnowledgeBaseService.Services.IEmbedderService, KnowledgeBaseService.Services.OnPremiseHashEmbedderService>();
    break;
}

// Register the KnowledgeBaseService as a singleton
builder.Services.AddSingleton<KnowledgeBaseService.Services.KnowledgeBaseService>();

var app = builder.Build();

// Load knowledge base at startup (embed all existing knowledge sources)
var knowledgeBaseService = app.Services.GetRequiredService<KnowledgeBaseService.Services.KnowledgeBaseService>();
Console.WriteLine("Loading knowledge base at startup...");
knowledgeBaseService.LoadKnowledgeBase();
Console.WriteLine($"Knowledge base loaded with {knowledgeBaseService.GetChunkCount()} chunks from {knowledgeBaseService.GetSourceCount()} sources");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthorization();
app.MapControllers();

app.Run();