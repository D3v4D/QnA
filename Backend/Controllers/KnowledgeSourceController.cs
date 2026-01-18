using KnowledgeBaseService.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeBaseService.Controllers;

/// <summary>
/// Controller for managing knowledge source uploads (files and URLs)
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class KnowledgeSourceController : ControllerBase
{
    private readonly Services.KnowledgeBaseService _knowledgeBaseService;
    private readonly string _knowledgeSourcesPath;
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the KnowledgeSourceController
    /// </summary>
    public KnowledgeSourceController(Services.KnowledgeBaseService knowledgeBaseService, IHttpClientFactory httpClientFactory)
    {
        _knowledgeBaseService = knowledgeBaseService;
        _knowledgeSourcesPath = Path.Combine(AppContext.BaseDirectory, "KnowledgeSources");
        
        // Create HttpClient with proper configuration
        var handler = new HttpClientHandler
        {
            AutomaticDecompression = System.Net.DecompressionMethods.All,
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 5
        };
        
        _httpClient = new HttpClient(handler);
        
        // Configure HttpClient to avoid 403 Forbidden errors
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        _httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
        _httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
        _httpClient.DefaultRequestHeaders.Add("Connection", "keep-alive");
        _httpClient.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        
        // Ensure the directory exists with proper permissions
        if (!Directory.Exists(_knowledgeSourcesPath))
        {
            Directory.CreateDirectory(_knowledgeSourcesPath);
        }
        
        Console.WriteLine($"KnowledgeSources path: {_knowledgeSourcesPath}");
        Console.WriteLine($"Directory exists: {Directory.Exists(_knowledgeSourcesPath)}");
        Console.WriteLine($"Directory writable: {CheckDirectoryWritable(_knowledgeSourcesPath)}");
    }
    
    private static bool CheckDirectoryWritable(string path)
    {
        try
        {
            var testFile = Path.Combine(path, ".write_test");
            System.IO.File.WriteAllText(testFile, "test");
            System.IO.File.Delete(testFile);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Upload a PDF or TXT file to the knowledge base
    /// </summary>
    /// <param name="file">The file to upload</param>
    /// <returns>Upload result</returns>
    [HttpPost("upload-file")]
    [RequestSizeLimit(104857600)] // 100 MB
    [ProducesResponseType(typeof(UploadKnowledgeSourceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadFile(IFormFile file)
    {
        try
        {
            Console.WriteLine($"Upload file request received");
            
            if (file == null || file.Length == 0)
            {
                Console.WriteLine("No file provided or file is empty");
                return BadRequest(new UploadKnowledgeSourceResponse
                {
                    Success = false,
                    Message = "No file provided or file is empty"
                });
            }

            Console.WriteLine($"File received: {file.FileName}, Size: {file.Length} bytes");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".pdf" && extension != ".txt")
            {
                Console.WriteLine($"Invalid file type: {extension}");
                return BadRequest(new UploadKnowledgeSourceResponse
                {
                    Success = false,
                    Message = "Only PDF and TXT files are supported"
                });
            }

            var fileName = Path.GetFileName(file.FileName);
            var filePath = Path.Combine(_knowledgeSourcesPath, fileName);

            Console.WriteLine($"Saving to: {filePath}");

            // Check if directory is writable
            if (!CheckDirectoryWritable(_knowledgeSourcesPath))
            {
                Console.WriteLine($"Directory is not writable: {_knowledgeSourcesPath}");
                return StatusCode(500, new UploadKnowledgeSourceResponse
                {
                    Success = false,
                    Message = $"Directory is not writable: {_knowledgeSourcesPath}"
                });
            }

            // Save the file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            Console.WriteLine($"File uploaded successfully: {fileName}");

            // Reload knowledge base to include the new file
            _knowledgeBaseService.LoadKnowledgeBase();
            
            // Clear QnA cache so new queries use the updated knowledge base
            QnAController.ClearCacheInternal();
            Console.WriteLine("QnA cache cleared after file upload");

            return Ok(new UploadKnowledgeSourceResponse
            {
                Success = true,
                Message = "File uploaded successfully",
                FileName = fileName
            });
        }
        catch (IOException ex)
        {
            Console.WriteLine($"IO Error uploading file: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return StatusCode(500, new UploadKnowledgeSourceResponse
            {
                Success = false,
                Message = $"Error saving file: {ex.Message}"
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error uploading file: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return StatusCode(500, new UploadKnowledgeSourceResponse
            {
                Success = false,
                Message = $"Error uploading file: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Download content from a URL and save it as an HTML file
    /// </summary>
    /// <param name="request">Request containing the URL</param>
    /// <returns>Upload result</returns>
    [HttpPost("upload-url")]
    [ProducesResponseType(typeof(UploadKnowledgeSourceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadUrl([FromBody] UploadKnowledgeSourceRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
        {
            return BadRequest(new UploadKnowledgeSourceResponse
            {
                Success = false,
                Message = "URL is required"
            });
        }

        if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var uri))
        {
            return BadRequest(new UploadKnowledgeSourceResponse
            {
                Success = false,
                Message = "Invalid URL format"
            });
        }

        try
        {
            Console.WriteLine($"Attempting to download from: {uri}");
            
            // Download the content
            var response = await _httpClient.GetAsync(uri);
            
            Console.WriteLine($"Response status: {response.StatusCode}");
            Console.WriteLine($"Response headers: {response.Headers}");
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Error response content: {errorContent.Substring(0, Math.Min(500, errorContent.Length))}");
                
                return StatusCode((int)response.StatusCode, new UploadKnowledgeSourceResponse
                {
                    Success = false,
                    Message = $"Failed to download URL: {response.StatusCode} - {response.ReasonPhrase}"
                });
            }

            var content = await response.Content.ReadAsStringAsync();
            
            if (string.IsNullOrWhiteSpace(content))
            {
                return BadRequest(new UploadKnowledgeSourceResponse
                {
                    Success = false,
                    Message = "Downloaded content is empty"
                });
            }

            Console.WriteLine($"Downloaded {content.Length} characters");

            // Generate a filename from the URL
            var fileName = GenerateFileNameFromUrl(uri);
            var filePath = Path.Combine(_knowledgeSourcesPath, fileName);

            Console.WriteLine($"Saving to: {filePath}");

            // Check if directory is writable
            if (!CheckDirectoryWritable(_knowledgeSourcesPath))
            {
                return StatusCode(500, new UploadKnowledgeSourceResponse
                {
                    Success = false,
                    Message = $"Directory is not writable: {_knowledgeSourcesPath}"
                });
            }

            // Save as HTML file
            await System.IO.File.WriteAllTextAsync(filePath, content);

            Console.WriteLine($"URL content downloaded successfully: {fileName}");

            // Reload knowledge base to include the new file
            _knowledgeBaseService.LoadKnowledgeBase();
            
            // Clear QnA cache so new queries use the updated knowledge base
            QnAController.ClearCacheInternal();
            Console.WriteLine("QnA cache cleared after URL upload");

            return Ok(new UploadKnowledgeSourceResponse
            {
                Success = true,
                Message = "URL content downloaded successfully",
                FileName = fileName
            });
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"HTTP Error downloading URL: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return StatusCode(500, new UploadKnowledgeSourceResponse
            {
                Success = false,
                Message = $"Error downloading URL: {ex.Message}"
            });
        }
        catch (IOException ex)
        {
            Console.WriteLine($"IO Error saving file: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return StatusCode(500, new UploadKnowledgeSourceResponse
            {
                Success = false,
                Message = $"Error saving file: {ex.Message}"
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error processing URL: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return StatusCode(500, new UploadKnowledgeSourceResponse
            {
                Success = false,
                Message = $"Error processing URL: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Generates a safe filename from a URL
    /// </summary>
    private static string GenerateFileNameFromUrl(Uri uri)
    {
        var host = uri.Host.Replace("www.", "");
        var path = uri.AbsolutePath.Trim('/');
        
        if (string.IsNullOrWhiteSpace(path))
        {
            path = "index";
        }
        else
        {
            // Clean up the path to make a valid filename
            path = path.Replace('/', '-');
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                path = path.Replace(c, '-');
            }
        }

        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        return $"{host}-{path}-{timestamp}.html";
    }
}

