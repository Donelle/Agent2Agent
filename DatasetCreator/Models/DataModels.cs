namespace DatasetCreator.Models;

public class DocumentChunk
{
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public Dictionary<string, string> Metadata { get; set; } = new();
    public float[]? Embedding { get; set; }
}

public class ProcessingResult
{
    public int FilesProcessed { get; set; }
    public int CsvRecords { get; set; }
    public int PdfChunks { get; set; }
    public int TotalEmbeddings { get; set; }
    public int Errors { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public List<string> ErrorMessages { get; set; } = new();
}

public class VehicleRegistrationRecord
{
    public string State { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;

	public override string ToString()
	{
		return $"State: {State}, DocumentType: {DocumentType}, Title: {Title}, Content: {Content}, SourceUrl: {SourceUrl}";
	}
}
