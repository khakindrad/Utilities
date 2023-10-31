using DirectorySerializor.Common;

namespace DirectorySerializor;
internal sealed class JsonRecord
{
    public required RecordType RecordType { get; set; }
    public required string Path { get; set; }
    public required string Name { get; set; }
    public string? Value { get; set; }

    public override string ToString()
    {
        return string.Join('|', RecordType, Path, Name, Value);
    }
}
