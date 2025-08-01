using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace DatasetCreator.Services;

public interface IFileProcessorService
{
    Task<List<DocumentChunk>> ProcessFileAsync(string filePath, CancellationToken cancellationToken = default);
    Task<List<DocumentChunk>> ProcessCsvAsync(string filePath, CancellationToken cancellationToken = default);
    Task<List<DocumentChunk>> ProcessPdfAsync(string filePath, CancellationToken cancellationToken = default);
}

public class FileProcessorService : IFileProcessorService
{
    private readonly ILogger<FileProcessorService> _logger;
    private readonly IConfiguration _configuration;

    public FileProcessorService(ILogger<FileProcessorService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<List<DocumentChunk>> ProcessFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        
        return extension switch
        {
            ".csv" => await ProcessCsvAsync(filePath, cancellationToken),
            ".pdf" => await ProcessPdfAsync(filePath, cancellationToken),
            _ => throw new NotSupportedException($"File format {extension} is not supported")
        };
    }

    public async Task<List<DocumentChunk>> ProcessCsvAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var chunks = new List<DocumentChunk>();
        
        try
        {
            _logger.LogInformation("Processing CSV file: {FilePath}", filePath);
            
            using var reader = new StringReader(await File.ReadAllTextAsync(filePath, cancellationToken));
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null
            });

            var records = csv.GetRecords<VehicleRegistrationRecord>().ToList();
            
            foreach (var record in records)
            {
                if (string.IsNullOrWhiteSpace(record.Content))
                {
                    _logger.LogWarning("Skipping record with empty content: {Title}", record.Title);
                    continue;
                }

                var chunk = new DocumentChunk
                {
                    Id = Guid.NewGuid().ToString(),
                    Text = record.Content,
                    Metadata = new Dictionary<string, string>
                    {
                        ["state"] = record.State,
                        ["documentType"] = record.DocumentType,
                        ["title"] = record.Title,
                        ["sourceUrl"] = record.SourceUrl,
                        ["sourceFile"] = Path.GetFileName(filePath)
                    }
                };
                
                chunks.Add(chunk);
            }

            _logger.LogInformation("Processed {Count} records from CSV file: {FilePath}", chunks.Count, filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing CSV file: {FilePath}", filePath);
            throw;
        }

        return chunks;
    }

    public async Task<List<DocumentChunk>> ProcessPdfAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var chunks = new List<DocumentChunk>();
        
        try
        {
            _logger.LogInformation("Processing PDF file: {FilePath}", filePath);
            
            using var pdfReader = new PdfReader(filePath);
            using var pdfDocument = new PdfDocument(pdfReader);
            
            var fullText = new StringBuilder();
            var pageCount = pdfDocument.GetNumberOfPages();
            
            for (int pageNum = 1; pageNum <= pageCount; pageNum++)
            {
                var page = pdfDocument.GetPage(pageNum);
                var pageText = PdfTextExtractor.GetTextFromPage(page);
                fullText.AppendLine(pageText);
            }

            var text = fullText.ToString();
            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogWarning("No text extracted from PDF: {FilePath}", filePath);
                return chunks;
            }

            // Chunk the text
            var textChunks = ChunkText(text, 
                _configuration.GetValue<int>("Processing:ChunkSize", 1000),
                _configuration.GetValue<int>("Processing:ChunkOverlap", 200));

            for (int i = 0; i < textChunks.Count; i++)
            {
                var chunk = new DocumentChunk
                {
                    Id = Guid.NewGuid().ToString(),
                    Text = textChunks[i],
                    Metadata = new Dictionary<string, string>
                    {
                        ["state"] = "unknown", // PDF files don't have state info by default
                        ["documentType"] = "PDF Document",
                        ["title"] = Path.GetFileNameWithoutExtension(filePath),
                        ["sourceUrl"] = filePath,
                        ["sourceFile"] = Path.GetFileName(filePath),
                        ["chunkIndex"] = i.ToString()
                    }
                };
                
                chunks.Add(chunk);
            }

            _logger.LogInformation("Processed PDF file: {FilePath}, Created {Count} chunks", filePath, chunks.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PDF file: {FilePath}", filePath);
            throw;
        }

        return chunks;
    }

    private static List<string> ChunkText(string text, int chunkSize, int overlap)
    {
        var chunks = new List<string>();
        var sentences = text.Split('.', StringSplitOptions.RemoveEmptyEntries);
        
        if (sentences.Length == 0)
            return chunks;

        var currentChunk = new StringBuilder();
        var currentLength = 0;

        foreach (var sentence in sentences)
        {
            var trimmedSentence = sentence.Trim();
            if (string.IsNullOrEmpty(trimmedSentence))
                continue;

            // Add the sentence with period back
            var sentenceWithPeriod = trimmedSentence + ".";
            
            if (currentLength + sentenceWithPeriod.Length > chunkSize && currentChunk.Length > 0)
            {
                // Save current chunk
                chunks.Add(currentChunk.ToString().Trim());
                
                // Start new chunk with overlap
                var overlapText = GetOverlapText(currentChunk.ToString(), overlap);
                currentChunk.Clear();
                currentChunk.Append(overlapText);
                currentLength = overlapText.Length;
            }

            currentChunk.Append(sentenceWithPeriod).Append(' ');
            currentLength += sentenceWithPeriod.Length + 1;
        }

        // Add the last chunk if it has content
        if (currentChunk.Length > 0)
        {
            chunks.Add(currentChunk.ToString().Trim());
        }

        return chunks;
    }

    private static string GetOverlapText(string text, int overlapSize)
    {
        if (text.Length <= overlapSize)
            return text;

        // Try to find a good break point near the overlap size
        var startIndex = Math.Max(0, text.Length - overlapSize);
        var spaceIndex = text.IndexOf(' ', startIndex);
        
        if (spaceIndex != -1 && spaceIndex < text.Length - 50)
            return text[spaceIndex..].Trim();
        
        return text[startIndex..].Trim();
    }
}

// Data model classes
public class DocumentChunk
{
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public Dictionary<string, string> Metadata { get; set; } = new();
    public float[]? Embedding { get; set; }
}

public class VehicleRegistrationRecord
{
    public string State { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
}