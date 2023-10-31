using System.Text;
using Common.Extensions;
using DirectorySerializor.Common;
using DirectorySerializor.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog;

namespace DirectorySerializor;

#pragma warning disable S1118 // Utility classes should not have public constructors
internal sealed class Program
#pragma warning restore S1118 // Utility classes should not have public constructors
{
    private static ILogger<Program>? _logger;
    private static async Task Main()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", false, true)
            .Build();

        var serviceCollection = new ServiceCollection()
                        .AddLogging(builder => builder.AddSerilog(
                            new LoggerConfiguration()
                                .ReadFrom.Configuration(config)
                                .CreateLogger()))
                        .BuildServiceProvider();

        _logger = serviceCollection.GetService<ILogger<Program>>();

        //Log.Logger = new LoggerConfiguration()
        //    .ReadFrom.Configuration(config)
        //    .CreateLogger();

        var appConfig = config.GetSection(ApplicationConfig.Name).Get<ApplicationConfig>();

        if (appConfig is not null)
        {
            _logger?.LogMessage($"Application starting in {appConfig.AppMode} mode.");

            if (appConfig.SrcPath is null || string.IsNullOrEmpty(appConfig.SrcPath))
            {
                _logger?.LogMessage("Unable to start application due to source path is null/empty.");
                return;
            }

            if (appConfig.AppMode == AppMode.PACK)
            {
                var excludeFolders = appConfig.ExcludeDir.Split(';', StringSplitOptions.RemoveEmptyEntries);
                var excludeFiles = appConfig.ExcludeFiles.Split(';', StringSplitOptions.RemoveEmptyEntries);

                var dirInfo = new DirectoryInfo(appConfig.SrcPath);

                var jsonDirInfo = new JsonDirInfo(appConfig.SrcPath)
                {
                    RootDir = dirInfo.Name
                };

                await ProcessDirectory(jsonDirInfo, dirInfo, excludeFolders, excludeFiles).ConfigureAwait(false);

                var logObj = JsonConvert.SerializeObject(jsonDirInfo);

                var targetFilePath = appConfig.TargetPath;

                if (string.IsNullOrEmpty(appConfig.TargetPath))
                {
                    targetFilePath = $"{appConfig.SrcPath}.json";
                }

                using var textWriter = new StreamWriter(targetFilePath);
                await textWriter.WriteAsync(logObj).ConfigureAwait(false);

                _logger?.LogMessage($"File created at {targetFilePath}");
            }
            else if (appConfig.AppMode == AppMode.UNPACK)
            {
                using var textReader = new StreamReader(appConfig.SrcPath);

                var fileContent = await textReader.ReadToEndAsync().ConfigureAwait(false);

                var objDirInfo = JsonConvert.DeserializeObject<JsonDirInfo>(fileContent);

                var targetFilePath = appConfig.TargetPath;

                if (string.IsNullOrEmpty(appConfig.TargetPath))
                {
                    targetFilePath = Path.GetDirectoryName(appConfig.SrcPath);
                }

                if (objDirInfo is not null && targetFilePath is not null)
                {
                    targetFilePath = Path.Combine(targetFilePath, objDirInfo.RootDir);

                    CreateDirectory(targetFilePath);

                    foreach (var record in objDirInfo.Records)
                    {
                        var path = Path.GetFullPath(record.Path, targetFilePath);

                        if (record.RecordType == RecordType.DIR)
                        {
                            CreateDirectory(path);
                        }
                        else if (record.RecordType == RecordType.FILE)
                        {
                            await CreateFile(Path.Combine(path, record.Name), record.Value).ConfigureAwait(false);
                        }
                    }
                }
                else
                {
                    _logger?.LogMessage($"Error while parsing object.");
                }

                _logger?.LogMessage($"File unpacked at {targetFilePath}");
            }
        }
        else
        {
            _logger?.LogMessage("Unable to start application due to appConfig is null.");
        }

        Console.ReadLine();
    }

    private static async Task ProcessDirectory(JsonDirInfo jsonDirInfo, DirectoryInfo dirInfo, string[] excludeFolders, string[] excludeFiles)
    {
        foreach (var dir in dirInfo.GetDirectories("*", SearchOption.TopDirectoryOnly))
        {
#pragma warning disable MA0002
            if (excludeFolders.Length > 0 && excludeFolders.Contains(dir.Name))
#pragma warning restore MA0002
            {
                continue;
            }

            var record = new JsonRecord()
            {
                RecordType = RecordType.DIR,
                Path = Path.GetRelativePath(jsonDirInfo.GetBasePath(), dir.FullName),
                Name = dir.Name
            };

            jsonDirInfo.AddRecord(record);

            _logger?.LogMessage(LogLevel.Debug, record.ToString());

            await ProcessDirectory(jsonDirInfo, dir, excludeFolders, excludeFiles).ConfigureAwait(false);
        }

        await ProcessFiles(jsonDirInfo, dirInfo, excludeFiles).ConfigureAwait(false);
    }

    private static async Task ProcessFiles(JsonDirInfo jsonDirInfo, DirectoryInfo directoryInfo, string[] excludeFiles)
    {
        var sbFileContent = new StringBuilder();

        foreach (var file in directoryInfo.GetFiles())
        {
#pragma warning disable MA0002
            if (excludeFiles.Length > 0 && excludeFiles.Contains(file.Name))
#pragma warning restore MA0002
            {
                continue;
            }
            sbFileContent.Clear();
            using (var stream = new StreamReader(file.FullName))
            {
                sbFileContent.Append(await stream.ReadToEndAsync().ConfigureAwait(false));
            }
            var record = new JsonRecord()
            {
                RecordType = RecordType.FILE,
                Path = Path.GetRelativePath(jsonDirInfo.GetBasePath(), directoryInfo.FullName),
                Name = file.Name,
                Value = sbFileContent.ToString()
            };
            _logger?.LogMessage(LogLevel.Debug, record.ToString());
            jsonDirInfo.AddRecord(record);
        }
    }

    private static void CreateDirectory(string directoryName)
    {
        if (!Directory.Exists(directoryName))
        {
            Directory.CreateDirectory(directoryName);
        }
    }

    private static async Task CreateFile(string filePath, string? content)
    {
        using var textWriter = new StreamWriter(filePath);

        await textWriter.WriteAsync(content).ConfigureAwait(false);
    }
}
