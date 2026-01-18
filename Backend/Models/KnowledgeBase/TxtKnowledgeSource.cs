namespace KnowledgeBaseService.Models;

/// <summary>
/// Knowledge source implementation for plain text (.txt) files
/// </summary>
public class TxtKnowledgeSource: IKnowledgeSource
{
    private String filePath;
    
    /// <summary>
    /// Initializes a new instance of the TxtKnowledgeSource class
    /// </summary>
    /// <param name="filePath">Path to the text file</param>
    public TxtKnowledgeSource(String filePath)
    {
        this.filePath = filePath;
    }
    
    /// <summary>
    /// Reads and returns the complete text content from the file
    /// </summary>
    /// <returns>The text content of the file</returns>
    /// <exception cref="FileNotFoundException">Thrown when the file doesn't exist or path is invalid</exception>
    public string GetContent()
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            throw new FileNotFoundException("TXT file not found.", filePath);
        return System.IO.File.ReadAllText(filePath);
    }
}