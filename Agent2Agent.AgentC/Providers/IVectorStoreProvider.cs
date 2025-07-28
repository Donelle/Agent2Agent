using NRedisStack.RedisStackCommands;
using NRedisStack.Search;
using static NRedisStack.Search.Schema.VectorField;

using StackExchange.Redis;

namespace Agent2Agent.AgentC.Providers;

/// <summary>
/// Represents a document chunk with its text content and associated metadata.
/// This record is used to encapsulate the text of a document chunk along with any metadata
/// that may be relevant for indexing or querying purposes.
/// </summary>
/// <param name="Text">The text content of the document chunk.</param>
/// <param name="Metadata">A dictionary of metadata associated with the document chunk.</param>
public record Chunk(string Text, IDictionary<string, string> Metadata);


/// <summary>
/// Interface for a vector store provider that manages document chunks and their embeddings.
/// This interface defines methods for upserting document chunks with their embeddings and metadata,
/// as well as querying for similar chunks based on a given embedding.
/// </summary>
public interface IVectorStoreProvider
{
	/// <summary>
	/// Indexes a document chunk with its embedding and metadata.
	/// </summary>
	Task UpsertChunkAsync(string id, string text, float[] embedding, IDictionary<string, string> metadata, CancellationToken cancellationToken = default);

	/// <summary>
	/// Returns the top-k most similar chunks to the given query embedding,
	/// optionally filtered by a similarity threshold.
	/// </summary>
	Task<IReadOnlyList<Chunk>> QuerySimilarAsync(float[] queryEmbedding, int topK, double threshold, CancellationToken cancellationToken = default);
}


/// <summary>
/// Redis-based vector store provider for managing document chunks and their embeddings.
/// This provider uses Redis Stack to store and query vector embeddings efficiently.
/// It supports upserting document chunks with their embeddings and metadata,
/// as well as querying for similar chunks based on a given embedding.
/// </summary>
internal class RedisVectorStoreProvider : IVectorStoreProvider, IDisposable
{
	private readonly ConnectionMultiplexer _connection;
	private readonly ILogger<RedisVectorStoreProvider> _logger;
	private readonly IDatabase _database;
	private bool disposedValue;

	public RedisVectorStoreProvider(ConnectionMultiplexer connection, ILogger<RedisVectorStoreProvider> logger)
	{
		_connection = connection;
		_database = _connection.GetDatabase();
		_logger = logger;
	}

	public async Task UpsertChunkAsync(string id, string text, float[] embedding, IDictionary<string, string> metadata, CancellationToken cancellationToken = default)
	{
		// Implementation for upserting a chunk into Redis
		var hash = new HashEntry[]
		{
			new HashEntry("text", text),
      // serialize float[] to byte[] for the embedding field
      new HashEntry("embedding", SerializeFloatArray(embedding)),
			new HashEntry("state", metadata["state"]),
			new HashEntry("sourceUrl", metadata["sourceUrl"])
		};

		await _database.HashSetAsync(new RedisKey($"doc:{id}"), hash);
	}

	public async Task<IReadOnlyList<Chunk>> QuerySimilarAsync(float[] queryEmbedding, int topK, double threshold, CancellationToken cancellationToken = default)
	{
		var knnArgs = new Dictionary<string, object> { ["vec"] = SerializeFloatArray(queryEmbedding) };
        var query = new Query($"*=>[KNN {topK} @embedding $vec]")
				.Params(knnArgs)
				.ReturnFields("text", "state", "sourceUrl");
		try
		{
			var res = await _database.FT().SearchAsync("vehicle_docs_idx", query);
			return res.Documents
				.Select(doc => new Chunk(
						doc["text"]!,
						new Dictionary<string, string>
						{
							["state"] = doc["state"]!,
							["sourceUrl"] = doc["sourceUrl"]!
						}
				))
				.Where((_, i) => res.Documents[i].Score >= threshold)
				.ToList();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error querying similar chunks in Redis.");
			return Array.Empty<Chunk>();
		}
	}

	internal async Task EnsureIndexExistsAsync()
	{
		var ft = _database.FT();
		await ft.InfoAsync("vehicle_docs_idx").ContinueWith(async t =>
		{
			if (t.IsFaulted || t.Result == null)
			{
				_logger.LogInformation("Index 'vehicle_docs_idx' does not exist, creating it.");
				var schema = new Schema();
				schema.AddVectorField("embedding", VectorAlgo.FLAT, new Dictionary<string, object>
				{
					["TYPE"] = "FLOAT32",
					["DIM"] = 1536,
					["DISTANCE_METRIC"] = "COSINE"
				});

				schema.AddTextField("text");
				schema.AddTagField("state");
				schema.AddTagField("sourceUrl");

				await ft.CreateAsync("vehicle_docs_idx", schema);
			}
			else
			{
				_logger.LogInformation("Index 'vehicle_docs_idx' already exists.");
			}
		});
	}

	/// <summary>
	/// Serializes a float array into a byte array (little-endian IEEE 754).
	/// </summary>
	public static byte[] SerializeFloatArray(float[] values)
	{
		// Allocate a byte buffer 4× as large as the float array
		byte[] result = new byte[values.Length * sizeof(float)];

		// Copy the raw float bytes into the byte array
		Buffer.BlockCopy(values, 0, result, 0, result.Length);

		return result;
	}

	/// <summary>
	/// Deserializes a byte array back into a float array.
	/// </summary>
	public static float[] DeserializeFloatArray(byte[] bytes)
	{
		if (bytes.Length % sizeof(float) != 0)
			throw new ArgumentException("Byte array length must be a multiple of 4.", nameof(bytes));

		int count = bytes.Length / sizeof(float);
		float[] result = new float[count];

		Buffer.BlockCopy(bytes, 0, result, 0, bytes.Length);

		return result;
	}

	#region IDisposable Support

	protected virtual void Dispose(bool disposing)
	{
		if (!disposedValue)
		{
			if (disposing)
			{
				_connection.Dispose();
			}

			disposedValue = true;
		}
	}


	~RedisVectorStoreProvider()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(disposing: false);
	}

	public void Dispose()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	#endregion
}