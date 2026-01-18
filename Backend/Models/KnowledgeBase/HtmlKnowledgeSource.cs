namespace KnowledgeBaseService.Models;
    
using HtmlAgilityPack;
using System.Text;

/// <summary>
/// Knowledge source implementation for HTML files using HtmlAgilityPack
/// </summary>
/// <remarks>
/// Extracts text content from HTML by removing scripts, styles, and other non-text elements,
/// then normalizing whitespace for cleaner text output
/// </remarks>
public class HtmlKnowledgeSource : IKnowledgeSource
{
    /// <summary>
    /// Utility class for converting HTML to plain text
    /// </summary>
    private static class HtmlUtils
    {
        /// <summary>
        /// Converts HTML content to plain text by removing tags and normalizing whitespace
        /// </summary>
        /// <param name="html">The HTML content to convert</param>
        /// <returns>Plain text extracted from the HTML</returns>
        public static string HtmlToText(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Remove script, style, and noscript elements
            doc.DocumentNode
                .SelectNodes("//script|//style|//noscript")
                ?.ToList()
                .ForEach(n => n.Remove());

            var sb = new StringBuilder();
            ExtractText(doc.DocumentNode, sb);

            return NormalizeWhitespace(sb.ToString());
        }

        /// <summary>
        /// Recursively extracts text from HTML nodes
        /// </summary>
        /// <param name="node">The HTML node to extract text from</param>
        /// <param name="sb">StringBuilder to accumulate the extracted text</param>
        private static void ExtractText(HtmlNode node, StringBuilder sb)
        {
            if (node.NodeType == HtmlNodeType.Text)
            {
                var text = ((HtmlTextNode)node).Text;
                if (!string.IsNullOrWhiteSpace(text))
                    sb.AppendLine(text.Trim());
            }

            foreach (var child in node.ChildNodes)
                ExtractText(child, sb);
        }

        /// <summary>
        /// Normalizes whitespace by trimming lines and removing empty lines
        /// </summary>
        /// <param name="text">The text to normalize</param>
        /// <returns>Normalized text with cleaned whitespace</returns>
        private static string NormalizeWhitespace(string text)
        {
            return string.Join(
                Environment.NewLine,
                text.Split('\n')
                    .Select(l => l.Trim())
                    .Where(l => l.Length > 0)
            );
        }
    }

    private readonly string _localHtmlPath;

    /// <summary>
    /// Initializes a new instance of the HtmlKnowledgeSource class
    /// </summary>
    /// <param name="localHtmlPath">Path to the local HTML file</param>
    public HtmlKnowledgeSource(string localHtmlPath)
    {
        _localHtmlPath = localHtmlPath;
    }

    /// <summary>
    /// Extracts and returns the text content from the HTML file
    /// </summary>
    /// <returns>Plain text extracted from the HTML file</returns>
    /// <exception cref="FileNotFoundException">Thrown when the HTML file doesn't exist</exception>
    public string GetContent()
    {
        var html =  File.ReadAllText(_localHtmlPath);
        return HtmlUtils.HtmlToText(html);
    }
}