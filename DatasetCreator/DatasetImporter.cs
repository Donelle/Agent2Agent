using DatasetCreator.Models;
using DatasetCreator.Services;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DatasetCreator;


public class DatasetImporter
{
	private readonly ILogger<DatasetImporter> _logger;
	private readonly IConfiguration _configuration;
	private readonly IEmbeddingService _embeddingService;
	private readonly IRedisService _redisService;
	private readonly IFileProcessorService _fileProcessor;
	

	public DatasetImporter(
			ILogger<DatasetImporter> logger,
			IConfiguration configuration,
			IEmbeddingService embeddingService,
			IRedisService redisService,
			IFileProcessorService fileProcessor)
	{
		_logger = logger;
		_configuration = configuration;
		_embeddingService = embeddingService;
		_redisService = redisService;
		_fileProcessor = fileProcessor;
	}

	public async Task ImportAsync(string inputPath, string[] formats, bool clearExisting)
	{
		var startTime = DateTime.UtcNow;
		var result = new ProcessingResult();

		try
		{
			_logger.LogInformation("Starting dataset import from: {InputPath}", inputPath);

			// Validate OpenAI API key
			var apiKey = _configuration["OpenAI:ApiKey"];
			if (string.IsNullOrWhiteSpace(apiKey))
			{
				_logger.LogError("OpenAI API key is required. Please set it in appsettings.json or environment variables.");
				return;
			}

			// Ensure Redis index exists
			if (!await _redisService.EnsureIndexExistsAsync())
			{
				_logger.LogError("Failed to ensure Redis index exists");
				return;
			}

			// Clear existing data if requested
			if (clearExisting)
			{
				if (!await _redisService.ClearExistingDataAsync())
				{
					_logger.LogError("Failed to clear existing data");
					return;
				}
			}

			// Get files to process
			var files = GetFilesToProcess(inputPath, formats);
			if (files.Count == 0)
			{
				_logger.LogWarning("No files found to process in: {InputPath}", inputPath);
				return;
			}

			_logger.LogInformation("Found {Count} files to process", files.Count);

			// Process files in batches
			var batchSize = _configuration.GetValue<int>("Processing:BatchSize", 50);
			var batches = files.Chunk(batchSize).ToList();

			foreach (var batch in batches)
			{
				await ProcessBatch(batch, result);
			}

			result.ProcessingTime = DateTime.UtcNow - startTime;
			PrintProcessingReport(result);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error during dataset import");
		}
	}

	public async Task ImportSingleFileAsync(string filePath, string state, int chunkSize, int chunkOverlap, bool clearExisting)
	{
		var startTime = DateTime.UtcNow;
		var result = new ProcessingResult();

		try
		{
			_logger.LogInformation("Starting single file import: {FilePath} with state: {State}", filePath, state);

			// Validate OpenAI API key
			var apiKey = _configuration["OpenAI:ApiKey"];
			if (string.IsNullOrWhiteSpace(apiKey))
			{
				_logger.LogError("OpenAI API key is required. Please set it in appsettings.json or environment variables.");
				return;
			}

			// Ensure Redis index exists
			if (!await _redisService.EnsureIndexExistsAsync())
			{
				_logger.LogError("Failed to ensure Redis index exists");
				return;
			}

			// Clear existing data if requested
			if (clearExisting)
			{
				if (!await _redisService.ClearExistingDataAsync())
				{
					_logger.LogError("Failed to clear existing data");
					return;
				}
			}

			// Process the single file with custom parameters
			_logger.LogDebug("Processing file: {File}", filePath);

			var chunks = await _fileProcessor.ProcessFileAsync(filePath, state, chunkSize, chunkOverlap);
			if (chunks.Count == 0)
			{
				_logger.LogWarning("No content extracted from file: {File}", filePath);
				return;
			}

			foreach (var chunk in chunks)
			{
				// Generate embedding
				var embedding = await _embeddingService.GetEmbeddingAsync(chunk.Text);
				if (embedding.Length == 0)
				{
					_logger.LogWarning("Failed to generate embedding for chunk: {Id}", chunk.Id);
					result.Errors++;
					continue;
				}

				// Store in Redis
				if (await _redisService.UpsertDocumentAsync(chunk.Id, chunk.Text, embedding, chunk.Metadata))
				{
					result.TotalEmbeddings++;
					result.PdfChunks++;
				}
				else
				{
					_logger.LogWarning("Failed to store chunk in Redis: {Id}", chunk.Id);
					result.Errors++;
				}
			}

			result.FilesProcessed = 1;
			_logger.LogInformation("Completed processing file: {File} ({Chunks} chunks)", filePath, chunks.Count);

			result.ProcessingTime = DateTime.UtcNow - startTime;
			PrintProcessingReport(result);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error during single file import: {File}", filePath);
		}
	}

	private async Task ProcessBatch(string[] files, ProcessingResult result)
	{
		var tasks = files.Select(async file =>
		{
			try
			{
				_logger.LogDebug("Processing file: {File}", file);

				var chunks = await _fileProcessor.ProcessFileAsync(file, string.Empty, 0, 0);
				if (chunks.Count == 0)
				{
					_logger.LogWarning("No content extracted from file: {File}", file);
					return;
				}

				foreach (var chunk in chunks)
				{
					// Generate embedding
					var embedding = await _embeddingService.GetEmbeddingAsync(chunk.Text);
					if (embedding.Length == 0)
					{
						_logger.LogWarning("Failed to generate embedding for chunk: {Id}", chunk.Id);
						result.Errors++;
						continue;
					}

					// Store in Redis
					if (await _redisService.UpsertDocumentAsync(chunk.Id, chunk.Text, embedding, chunk.Metadata))
					{
						result.TotalEmbeddings++;

						if (Path.GetExtension(file).ToLowerInvariant() == ".csv")
							result.CsvRecords++;
						else
							result.PdfChunks++;
					}
					else
					{
						_logger.LogWarning("Failed to store chunk in Redis: {Id}", chunk.Id);
						result.Errors++;
					}
				}

				result.FilesProcessed++;
				_logger.LogInformation("Completed processing file: {File} ({Chunks} chunks)", file, chunks.Count);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error processing file: {File}", file);
				result.Errors++;
				result.ErrorMessages.Add($"Error processing {file}: {ex.Message}");
			}
		});

		await Task.WhenAll(tasks);
	}

	private List<string> GetFilesToProcess(string inputPath, string[] formats)
	{
		var files = new List<string>();
		var extensions = formats.Select(f => $".{f.ToLowerInvariant()}").ToHashSet();

		if (File.Exists(inputPath))
		{
			var ext = Path.GetExtension(inputPath).ToLowerInvariant();
			if (extensions.Contains(ext))
			{
				files.Add(inputPath);
			}
		}
		else if (Directory.Exists(inputPath))
		{
			foreach (var ext in extensions)
			{
				files.AddRange(Directory.GetFiles(inputPath, $"*{ext}", SearchOption.AllDirectories));
			}
		}
		else
		{
			_logger.LogError("Input path does not exist: {InputPath}", inputPath);
		}

		return files;
	}

	private void PrintProcessingReport(ProcessingResult result)
	{
		_logger.LogInformation(
				 """
				=== DatasetCreator Processing Report ===
				Files Processed: {FilesProcessed}
					- CSV Files: {CsvFiles} ({CsvRecords} records)
					- PDF Files: {PdfFiles} ({PdfChunks} text chunks)
				Redis Documents Created: {TotalEmbeddings}
				Processing Time: {ProcessingTime:hh\:mm\:ss}
				Errors: {Errors}
				""",
				result.FilesProcessed,
				result.FilesProcessed - (result.PdfChunks > 0 ? 1 : 0),
				result.CsvRecords,
				result.PdfChunks > 0 ? 1 : 0,
				result.PdfChunks,
				result.TotalEmbeddings,
				result.ProcessingTime,
				result.Errors);

		if (result.ErrorMessages.Count > 0)
		{
			_logger.LogWarning("Error details:");
			foreach (var error in result.ErrorMessages)
			{
				_logger.LogWarning("  - {Error}", error);
			}
		}
	}
}

