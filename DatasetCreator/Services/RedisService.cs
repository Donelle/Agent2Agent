using StackExchange.Redis;
using NRedisStack.RedisStackCommands;
using NRedisStack.Search;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using static NRedisStack.Search.Schema;
using DatasetCreator.Models;

namespace DatasetCreator.Services;

public interface IRedisService
{
    Task<bool> UpsertDocumentAsync(string id, string text, float[] embedding, Dictionary<string, string> metadata, CancellationToken cancellationToken = default);
    Task<bool> EnsureIndexExistsAsync(CancellationToken cancellationToken = default);
    Task<bool> ClearExistingDataAsync(CancellationToken cancellationToken = default);
    Task<DocumentChunk?> GetDocumentChunkAsync(string chunkIndex, CancellationToken cancellationToken = default);
}

internal class RedisService : IRedisService, IDisposable
{
    private readonly ConnectionMultiplexer _connection;
    private readonly IDatabase _database;
    private readonly ILogger<RedisService> _logger;
    private bool _disposed;

    public RedisService(IConfiguration configuration, ILogger<RedisService> logger)
    {
        var connectionString = configuration["Redis:ConnectionString"] ?? "localhost:6379";
        _connection = ConnectionMultiplexer.Connect(connectionString);
        _database = _connection.GetDatabase();
        _logger = logger;
    }

    public async Task<DocumentChunk?> GetDocumentChunkAsync(string chunkIndex, CancellationToken cancellationToken = default)
    {
        try
        {
            var ft = _database.FT();
            var query = new Query($"@chunkIndex:{{{chunkIndex}}}").ReturnFields("content", "state", "documentType", "title", "sourceUrl", "documentId");
            var res = await ft.SearchAsync("vehicle_docs_idx", query);
            return res.Documents.Count != 0
                ? new DocumentChunk
                {
                    Id = chunkIndex,
                    Text = res.Documents[0]["content"].ToString(),
                    Metadata = new Dictionary<string, string>
                    {
                        ["state"] = res.Documents[0]["state"].ToString(),
                        ["documentType"] = res.Documents[0]["documentType"].ToString(),
                        ["title"] = res.Documents[0]["title"].ToString(),
                        ["sourceUrl"] = res.Documents[0]["sourceUrl"].ToString(),
                        ["documentId"] = res.Documents[0]["documentId"].ToString(),
                    }
                } 
                : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get document with chunkIndex: {chunkIndex}", chunkIndex);
            return null;
        }
    }


    public async Task<bool> EnsureIndexExistsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var ft = _database.FT();
            
            // Check if index exists
            try
            {
                await ft.InfoAsync("vehicle_docs_idx");
                _logger.LogInformation("Index 'vehicle_docs_idx' already exists");
                return true;
            }
            catch
            {
                // Index doesn't exist, create it
                _logger.LogInformation("Creating index 'vehicle_docs_idx'");
                
                var schema = new Schema();
                schema.AddVectorField("embedding", VectorField.VectorAlgo.FLAT, new Dictionary<string, object>
                {
                    ["TYPE"] = "FLOAT32",
                    ["DIM"] = 1536,
                    ["DISTANCE_METRIC"] = "COSINE"
                });

                schema.AddTextField("title", 2);
                schema.AddTextField("content");
                schema.AddTextField("state");
                schema.AddTextField("sourceUrl");
                schema.AddTextField("documentType");
                schema.AddTextField("documentId");
                schema.AddTextField("chunkIndex");

                await ft.CreateAsync("vehicle_docs_idx", schema);
                _logger.LogInformation("Successfully created index 'vehicle_docs_idx'");
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure index exists");
            return false;
        }
    }

    public async Task<bool> UpsertDocumentAsync(string id, string text, float[] embedding, Dictionary<string, string> metadata, CancellationToken cancellationToken = default)
    {
        try
        {
            await _database.ExecuteAsync("FT.ADD", new object[]
            {
                "vehicle_docs_idx", id, "1.0", "REPLACE", "FIELDS",
                "content", text,
                "embedding", SerializeFloatArray(embedding),
                "state", metadata.GetValueOrDefault("state", "unknown"),
                "sourceUrl", metadata.GetValueOrDefault("sourceUrl", "unknown"),
                "documentType", metadata.GetValueOrDefault("documentType", "unknown"),
                "title", metadata.GetValueOrDefault("title", "No Title"),
                "documentId", metadata.GetValueOrDefault("documentId", ""),
                "chunkIndex", metadata.GetValueOrDefault("chunkIndex", "0")
            });
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upsert document with ID: {Id}", id);
        }

        return false;
    }

    public async Task<bool> ClearExistingDataAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Clearing existing vector index data from Redis");
            // Drop the RediSearch index and all documents
            await _database.ExecuteAsync("FT.DROPINDEX", "vehicle_docs_idx", "DD");
            _logger.LogInformation("Dropped index 'vehicle_docs_idx' and deleted all documents");
            // Recreate the index
            var ft = _database.FT();
            var schema = new NRedisStack.Search.Schema();
            schema.AddVectorField("embedding", NRedisStack.Search.Schema.VectorField.VectorAlgo.FLAT, new Dictionary<string, object>
            {
                ["TYPE"] = "FLOAT32",
                ["DIM"] = 1536,
                ["DISTANCE_METRIC"] = "COSINE"
            });
            schema.AddTextField("title", 2);
            schema.AddTextField("content");
            schema.AddTextField("state");
            schema.AddTextField("sourceUrl");
            schema.AddTextField("documentType");
            schema.AddTextField("chunkIndex");
            schema.AddTextField("documentId");

            await ft.CreateAsync("vehicle_docs_idx", schema);
            _logger.LogInformation("Recreated index 'vehicle_docs_idx'");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear existing data");
            return false;
        }
    }

    private static byte[] SerializeFloatArray(float[] values)
    {
        byte[] result = new byte[values.Length * sizeof(float)];
        Buffer.BlockCopy(values, 0, result, 0, result.Length);
        return result;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _connection?.Dispose();
            _disposed = true;
        }
    }
}