using OpenAI.Embeddings;

namespace Agent2Agent.AgentC.Services;

public interface IEmbeddingProvider
{
    /// <summary>
    /// Given an arbitrary text input, returns its embedding as a float array.
    /// </summary>
    Task<float[]> GetEmbeddingAsync(string text, CancellationToken cancellationToken = default);
}

internal class OpenAIEmbeddingProvider : IEmbeddingProvider
{
    private readonly EmbeddingClient _client;
    private readonly ILogger<OpenAIEmbeddingProvider> _logger;

    public OpenAIEmbeddingProvider(IConfiguration config, ILogger<OpenAIEmbeddingProvider> logger)
    {
        _client = new EmbeddingClient(config.GetValue<string>("OpenAI:EmbeddingModel"), config.GetValue<string>("OpenAI:ApiKey"));
        _logger = logger;
    }

    public async Task<float[]> GetEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        var result = await _client.GenerateEmbeddingAsync(text, cancellationToken: cancellationToken);
        if (result is null || result.Value is null)
        {
            _logger.LogError("Failed to generate embedding.");
            return Array.Empty<float>();
        }

        return result.Value.ToFloats().ToArray();
    }
}
