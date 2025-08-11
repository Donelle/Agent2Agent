using System.Globalization;
using System.Text;

using CsvHelper;
using CsvHelper.Configuration;

using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;

using Microsoft.Extensions.Logging;

using DatasetCreator.Models;
using DatasetCreator.Shared;

namespace DatasetCreator.Services;

public interface IFileProcessorService
{
	Task<List<DocumentChunk>> ProcessFileAsync(string filePath, string state, int chunkSize, int chunkOverlap, CancellationToken cancellationToken = default);
}

public class FileProcessorService : IFileProcessorService
{
	private readonly ILogger<FileProcessorService> _logger;

	public FileProcessorService(ILogger<FileProcessorService> logger)
	{
		_logger = logger;
	}

	public async Task<List<DocumentChunk>> ProcessFileAsync(string filePath, string state, int chunkSize, int chunkOverlap, CancellationToken cancellationToken = default)
	{
		var extension = Path.GetExtension(filePath).ToLowerInvariant();

		return extension switch
		{
			".csv" => await ProcessCsvAsync(filePath, state, chunkSize, chunkOverlap, cancellationToken),
			".pdf" => ProcessPdf(filePath, state, chunkSize, chunkOverlap),
			_ => throw new NotSupportedException($"File format {extension} is not supported")
		};
	}

	private async Task<List<DocumentChunk>> ProcessCsvAsync(string fileName, string state, int chunkSize, int chunkOverlap, CancellationToken cancellationToken = default)
	{
		var chunks = new List<DocumentChunk>();

		try
		{
			_logger.LogInformation("Reading CSV file: {FilePath}", Path.GetFileName(fileName));

			using var reader = new StringReader(await File.ReadAllTextAsync(fileName, cancellationToken));
			using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
			{
				HasHeaderRecord = true,
				MissingFieldFound = null
			});

			var records = csv.GetRecords<VehicleRegistrationRecord>().ToList();
			var skipped = 0;
			foreach (var record in records)
			{
				if (string.IsNullOrWhiteSpace(record.Content))
				{
					_logger.LogWarning("Skipping record with empty content: {@Record}", record);
					skipped++;
					continue;
				}

				// Chunk the text with custom parameters
				var textChunks = ChunkText(record.Content, chunkSize, chunkOverlap);
				var documentId = GenerateId($"{record.State.Trim()}|{record.DocumentType.Trim()}|{record.Title.Trim()}");

				for (int i = 0; i < textChunks.Count; i++)
				{
					var chunk = new DocumentChunk
					{
						Id = GenerateId($"{documentId}|{textChunks[i]}"),
						Text = textChunks[i],
						Metadata = new Dictionary<string, string>
						{
							[RedisFieldNames.StateFieldName] = record.State,
							[RedisFieldNames.DocumentTypeFieldName] = record.DocumentType,
							[RedisFieldNames.TitleFieldName] = record.Title,
							[RedisFieldNames.SourceUrlFieldName] = record.SourceUrl,
							[RedisFieldNames.DocumentIdFieldName] = documentId,
						}
					};

					chunks.Add(chunk);
				}
			}

			_logger.LogInformation("Read {Count} records from CSV file: {FileName}, Created {Count} chunks", 
				records.Count - skipped, chunks.Count, fileName);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error reading CSV file: {FilePath}", fileName);
			throw;
		}

		return chunks;
	}

	private List<DocumentChunk> ProcessPdf(string filePath, string state, int chunkSize, int chunkOverlap)
	{
		var chunks = new List<DocumentChunk>();

		try
		{
			_logger.LogInformation("Processing PDF file: {FilePath} with state: {State}, chunk size: {ChunkSize}, overlap: {ChunkOverlap}",
					filePath, state, chunkSize, chunkOverlap);

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

			// Chunk the text with custom parameters
			var textChunks = ChunkText(text, chunkSize, chunkOverlap);
			var fileName = Path.GetFileName(filePath);
			var documentId = GenerateId($"{state}|PDFDOCUMENT|{fileName}");

			for (int i = 0; i < textChunks.Count; i++)
			{
				var chunk = new DocumentChunk
				{
					Id = GenerateId($"{documentId}|{textChunks[i]}"),
					Text = textChunks[i],
					Metadata = new Dictionary<string, string>
					{
						[RedisFieldNames.StateFieldName] = state, // Use the provided state
						[RedisFieldNames.DocumentTypeFieldName] = "PDF Document",
						[RedisFieldNames.TitleFieldName] = fileName,
						[RedisFieldNames.SourceUrlFieldName] = filePath,
						[RedisFieldNames.DocumentIdFieldName] = documentId
					}
				};

				chunks.Add(chunk);
			}

			_logger.LogInformation("Processed PDF file: {FilePath}, Created {Count} chunks with state: {State}",
					filePath, chunks.Count, state);
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

	private static string GenerateId(string key)
	{
		using var sha = System.Security.Cryptography.SHA256.Create();
		var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(key));
		return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
	}
}
