using OpenAI.Embeddings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace DatasetCreator.Services;

public interface IEmbeddingService
{
    Task<float[]> GetEmbeddingAsync(string text, CancellationToken cancellationToken = default);
}

public class OpenAIEmbeddingService : IEmbeddingService
{
    private readonly EmbeddingClient _client;
    private readonly ILogger<OpenAIEmbeddingService> _logger;

    public OpenAIEmbeddingService(IConfiguration configuration, ILogger<OpenAIEmbeddingService> logger)
    {
        var apiKey = configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI API key is required");
        var model = configuration["OpenAI:EmbeddingModel"] ?? "text-embedding-3-small";
        
        _client = new EmbeddingClient(model, apiKey);
        _logger = logger;
    }

    public async Task<float[]> GetEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _client.GenerateEmbeddingAsync(text, cancellationToken: cancellationToken);
            if (result?.Value == null)
            {
                _logger.LogError("Failed to generate embedding for text: {Text}", text[..Math.Min(text.Length, 100)]);
                return Array.Empty<float>();
            }

            return result.Value.ToFloats().ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embedding for text: {Text}", text[..Math.Min(text.Length, 100)]);
            return Array.Empty<float>();
        }
    }
}