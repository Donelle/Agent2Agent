namespace Agent2Agent.AgentC.Plugins;

internal class FactStoreService
{
    private readonly ILogger<FactStoreService> _logger;
    private readonly IVectorStoreProvider _vectorStoreProvider;
    private readonly IEmbeddingProvider _embeddingProvider;

    public FactStoreService(ILogger<FactStoreService> logger, IVectorStoreProvider vectorStoreProvider, IEmbeddingProvider embeddingProvider)
    {
        _embeddingProvider = embeddingProvider;
        _logger = logger;
        _vectorStoreProvider = vectorStoreProvider;
    }   
    
    public async Task<IReadOnlyList<string>> SearchKnowledgeBaseAsync(string query, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Searching knowledge base for query: {Query}", query);

        if (string.IsNullOrWhiteSpace(query))
        {
            _logger.LogWarning("Empty query provided to search knowledge base.");
            return Array.Empty<string>();
        }

        try
        {
            var qVecs = await _embeddingProvider.GetEmbeddingAsync(query, cancellationToken);
            var chunks = await _vectorStoreProvider.QuerySimilarAsync(
                qVecs,
                topK: 5,
                threshold: 0.78,
                cancellationToken: cancellationToken);
            return chunks.Select(c => c.Text).ToList().AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching knowledge base for query: {Query}", query);
            return Array.Empty<string>();
        }
    }
}