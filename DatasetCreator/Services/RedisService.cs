using DatasetCreator.Models;
using DatasetCreator.Shared;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using NRedisStack.RedisStackCommands;
using NRedisStack.Search;
using NRedisStack.Search.Literals.Enums;

using StackExchange.Redis;

using static NRedisStack.Search.Schema;

namespace DatasetCreator.Services;

public interface IRedisService
{
    Task<bool> InsertDocumentAsync(string id, string text, float[] embedding, Dictionary<string, string> metadata, CancellationToken cancellationToken = default);
    Task<bool> EnsureIndexExistsAsync(CancellationToken cancellationToken = default);
    Task<bool> ClearExistingDataAsync(CancellationToken cancellationToken = default);
    Task<DocumentChunk?> GetDocumentAsync(string chunkId, CancellationToken cancellationToken = default);
}

internal class RedisService : IRedisService, IDisposable
{
    private readonly ConnectionMultiplexer _connection;
    private readonly IDatabase _database;
    private readonly ILogger<RedisService> _logger;
    private bool _disposed;

    readonly string VECTOR_INDEX_NAME = "vehicle_docs_idx";

    public RedisService(IConfiguration configuration, ILogger<RedisService> logger)
    {
        var connectionString = configuration["Redis:ConnectionString"] ?? "localhost:6379";
        _connection = ConnectionMultiplexer.Connect(connectionString);
        _database = _connection.GetDatabase();
        _logger = logger;
    }

    public async Task<DocumentChunk?> GetDocumentAsync(string chunkId, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = 
                new Query($"@{RedisFieldNames.ChunkIndexFieldName}:\"{chunkId}\"")
                  .ReturnFields(
                    RedisFieldNames.ContentFieldName, 
                    RedisFieldNames.StateFieldName, 
                    RedisFieldNames.DocumentTypeFieldName, 
                    RedisFieldNames.TitleFieldName, 
                    RedisFieldNames.SourceUrlFieldName, 
                    RedisFieldNames.DocumentIdFieldName
                  );
            var res = await _database.FT().SearchAsync(VECTOR_INDEX_NAME, query);
            return res.Documents.Count != 0
                ? new DocumentChunk
                {
                    Id = chunkId,
                    Text = res.Documents[0][RedisFieldNames.ContentFieldName].ToString(),
                    Metadata = new Dictionary<string, string>
                    {
                        [RedisFieldNames.StateFieldName] = res.Documents[0][RedisFieldNames.StateFieldName].ToString(),
                        [RedisFieldNames.DocumentTypeFieldName] = res.Documents[0][RedisFieldNames.DocumentTypeFieldName].ToString(),
                        [RedisFieldNames.TitleFieldName] = res.Documents[0][RedisFieldNames.TitleFieldName].ToString(),
                        [RedisFieldNames.SourceUrlFieldName] = res.Documents[0][RedisFieldNames.SourceUrlFieldName].ToString(),
                        [RedisFieldNames.DocumentIdFieldName] = res.Documents[0][RedisFieldNames.DocumentIdFieldName].ToString()
										}
                } 
                : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get document with chunkId: {chunkId}", chunkId);
            return null;
        }
    }


    public async Task<bool> EnsureIndexExistsAsync(CancellationToken cancellationToken = default)
    {
		  var ft = _database.FT();

		  // Check if index exists
		  try
		  {
			  await ft.InfoAsync(VECTOR_INDEX_NAME);
			  _logger.LogInformation($"Index '{VECTOR_INDEX_NAME}' already exists");
			  return true;
		  }

			catch
		  {
			  // Index doesn't exist, create it
			  _logger.LogInformation($"Creating index '{VECTOR_INDEX_NAME}'");
			  return await _CreateIndexAsync();
		  }
	  }

    public async Task<bool> InsertDocumentAsync(string id, string text, float[] embedding, Dictionary<string, string> metadata, CancellationToken cancellationToken = default)
    {
        try
        {
            // Serialize the embedding into a byte array
            var serializedEmbedding = SerializeFloatArray(embedding);

            // Use HSET to insert the document into the Redis hash
            HashEntry [] hashFields = 
            {
                new(RedisFieldNames.ChunkIndexFieldName, id),
                new(RedisFieldNames.ContentFieldName, text),
                new(RedisFieldNames.EmbeddingFieldName, serializedEmbedding),
                new(RedisFieldNames.StateFieldName, metadata.GetValueOrDefault(RedisFieldNames.StateFieldName, "unknown")),
                new(RedisFieldNames.SourceUrlFieldName, metadata.GetValueOrDefault(RedisFieldNames.SourceUrlFieldName, "unknown")),
                new(RedisFieldNames.DocumentTypeFieldName, metadata.GetValueOrDefault(RedisFieldNames.DocumentTypeFieldName, "unknown")),
                new(RedisFieldNames.TitleFieldName, metadata.GetValueOrDefault(RedisFieldNames.TitleFieldName, "No Title")),
                new(RedisFieldNames.DocumentIdFieldName, metadata.GetValueOrDefault(RedisFieldNames.DocumentIdFieldName, ""))
            };

            await _database.HashSetAsync($"doc:{id}", hashFields.ToArray());
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to insert document with ID: {Id}", id);
            return false;
        }
    }

    public async Task<bool> ClearExistingDataAsync(CancellationToken cancellationToken = default)
    {
        try
		    {
			    _logger.LogInformation("Clearing existing vector index data from Redis");
			    // Drop the RediSearch index and all documents
			    await _database.ExecuteAsync("FT.DROPINDEX", VECTOR_INDEX_NAME, "DD");

			    _logger.LogInformation($"Dropped index '{VECTOR_INDEX_NAME}' and deleted all documents");
			    // Recreate the index
			    return await _CreateIndexAsync();
		    }

				catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear existing data");
            return false;
        }
    }

	  private async Task<bool> _CreateIndexAsync()
	  {
      var opts = new FTCreateParams().On(IndexDataType.HASH).Prefix("doc:");
		  var schema = new Schema();
		  schema.AddVectorField(RedisFieldNames.EmbeddingFieldName, VectorField.VectorAlgo.FLAT, new Dictionary<string, object>
		  {
			  ["TYPE"] = "FLOAT32",
			  ["DIM"] = 1536,
			  ["DISTANCE_METRIC"] = "COSINE"
		  });
      schema.AddTextField(RedisFieldNames.ChunkIndexFieldName, 1, false);
		  schema.AddTextField(RedisFieldNames.TitleFieldName, 2, true);
		  schema.AddTextField(RedisFieldNames.ContentFieldName);
		  schema.AddTextField(RedisFieldNames.StateFieldName, sortable: true);
		  schema.AddTextField(RedisFieldNames.SourceUrlFieldName);
		  schema.AddTextField(RedisFieldNames.DocumentTypeFieldName);
		  schema.AddTextField(RedisFieldNames.DocumentIdFieldName);

			return await _database.FT().CreateAsync(VECTOR_INDEX_NAME, opts, schema);
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