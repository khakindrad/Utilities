using DirectorySerializor.Common;

namespace DirectorySerializor.Options;
public sealed class ApplicationConfig
{
    public const string Name = "AppConfig";

    public required AppMode AppMode { get; set; }
    public required string SrcPath { get; set; }
    public required string ExcludeDir { get; set; }
    public required string ExcludeFiles { get; set; }
    public required string TargetPath { get; set; }
}
