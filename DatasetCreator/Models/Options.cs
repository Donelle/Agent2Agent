namespace DatasetCreator.Models;

public class ProcessingOptions
{
	public int ChunkSize { get; set; } = 1000;
	public int ChunkOverlap { get; set; } = 200;
	public int BatchSize { get; set; } = 50;
}

public class DataSourceOptions
{
	public string InputDirectory { get; set; } = "./Data";
	public string[] SupportedFormats { get; set; } = ["csv", "pdf"];
}

public class RedisOptions
{
	public string ConnectionString { get; set; } = "localhost:6379";
}

public class OpenAIOptions
{
	public string ApiKey { get; set; } = string.Empty;
	public string EmbeddingModel { get; set; } = "text-embedding-3-small";
}

