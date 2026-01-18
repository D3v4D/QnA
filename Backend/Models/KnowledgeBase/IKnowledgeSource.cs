namespace KnowledgeBaseService.Models;

/// <summary>
/// Interface for knowledge source implementations that can extract text content from various file formats
/// </summary>
public interface IKnowledgeSource
{
    /// <summary>
    /// Extracts and returns the text content from the knowledge source
    /// </summary>
    /// <returns>The extracted text content as a string</returns>
    public string GetContent();
}