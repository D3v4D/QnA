using System;
using System.IO;
using System.Text;
using UglyToad.PdfPig;

namespace KnowledgeBaseService.Models
{
    /// <summary>
    /// Knowledge source implementation for PDF files using PdfPig library
    /// </summary>
    public class PdfKnowledgeSource : IKnowledgeSource
    {
        private readonly string filePath;

        /// <summary>
        /// Initializes a new instance of the PdfKnowledgeSource class
        /// </summary>
        /// <param name="filePath">Path to the PDF file</param>
        public PdfKnowledgeSource(string filePath)
        {
            this.filePath = filePath;
        }

        /// <summary>
        /// Extracts and returns all text content from the PDF file
        /// </summary>
        /// <returns>The combined text content from all pages in the PDF</returns>
        /// <exception cref="FileNotFoundException">Thrown when the PDF file doesn't exist or path is invalid</exception>
        /// <remarks>
        /// Uses the PdfPig library to extract text from each page sequentially
        /// </remarks>
        public string GetContent()
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                throw new FileNotFoundException("PDF file not found.", filePath);

            var sb = new StringBuilder();
            using (var document = PdfDocument.Open(filePath))
            {
                foreach (var page in document.GetPages())
                {
                    sb.AppendLine(page.Text);
                }
            }

            return sb.ToString();
        }
    }
}