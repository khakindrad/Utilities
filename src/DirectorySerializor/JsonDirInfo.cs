namespace DirectorySerializor;

internal sealed class JsonDirInfo
{
    private readonly string _basePath;

    public required string RootDir { get; set; }
    public int NoOfRecords { get; private set; }
    public List<JsonRecord> Records { get; init; }

    public JsonDirInfo(string basePath)
    {
        _basePath = basePath;
        Records = new List<JsonRecord>();
    }

    public string GetBasePath() => _basePath;

    internal void AddRecord(JsonRecord record)
    {
        Records.Add(record);
        NoOfRecords++;
        //record.Dispose();
    }
}
