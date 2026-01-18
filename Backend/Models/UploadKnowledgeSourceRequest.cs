namespace KnowledgeBaseService.Models;

/// <summary>
/// Request model for uploading new knowledge sources
/// </summary>
public class UploadKnowledgeSourceRequest
{
    /// <summary>
    /// URL to download content from (optional)
    /// </summary>
    public string? Url { get; set; }
}

