using StackExchange.Redis;
using NRedisStack.RedisStackCommands;
using NRedisStack.Search;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using static NRedisStack.Search.Schema;

namespace DatasetCreator.Services;

public interface IRedisService
{
    Task<bool> UpsertDocumentAsync(string id, string text, float[] embedding, Dictionary<string, string> metadata, CancellationToken cancellationToken = default);
    Task<bool> EnsureIndexExistsAsync(CancellationToken cancellationToken = default);
    Task<bool> ClearExistingDataAsync(CancellationToken cancellationToken = default);
}

public class RedisService : IRedisService, IDisposable
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
                schema.AddTextField("text");
                schema.AddTextField("state");
                schema.AddTextField("sourceUrl");
                schema.AddTextField("documentType");

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
            var hash = new HashEntry[]
            {
                new("text", text),
                new("embedding", SerializeFloatArray(embedding)),
                new("state", metadata.GetValueOrDefault("state", "unknown")),
                new("sourceUrl", metadata.GetValueOrDefault("sourceUrl", "unknown")),
                new("documentType", metadata.GetValueOrDefault("documentType", "unknown")),
                new("title", metadata.GetValueOrDefault("title", "No Title"))
            };

            await _database.HashSetAsync($"doc:{id}", hash);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upsert document with ID: {Id}", id);
            return false;
        }
    }

    public async Task<bool> ClearExistingDataAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Clearing existing data from Redis");
            var server = _connection.GetServer(_connection.GetEndPoints().First());
            var keys = server.Keys(pattern: "doc:*").ToArray();
            
            if (keys.Length > 0)
            {
                await _database.KeyDeleteAsync(keys);
                _logger.LogInformation("Cleared {Count} existing documents", keys.Length);
            }
            
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