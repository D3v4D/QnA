namespace KnowledgeBaseService.Models;

/// <summary>
/// Response model for knowledge source upload operations
/// </summary>
public class UploadKnowledgeSourceResponse
{
    /// <summary>
    /// Indicates whether the upload was successful
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Message describing the result
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Name of the uploaded/downloaded file
    /// </summary>
    public string? FileName { get; set; }
}

