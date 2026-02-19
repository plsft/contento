using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using SharpGrip.FileSystem.Models;

namespace Contento.Services;

public class FileStorageService : IFileStorageService
{
    private readonly SharpGrip.FileSystem.IFileSystem _fileSystem;
    private readonly IConfiguration _configuration;
    private readonly ILogger<FileStorageService> _logger;
    private readonly string _prefix;

    public FileStorageService(
        SharpGrip.FileSystem.IFileSystem fileSystem,
        IConfiguration configuration,
        ILogger<FileStorageService> logger)
    {
        _fileSystem = fileSystem;
        _configuration = configuration;
        _logger = logger;

        var provider = _configuration["Storage:Provider"] ?? "local";
        var s3Endpoint = _configuration["Storage:S3:Endpoint"];
        _prefix = provider == "s3" && !string.IsNullOrEmpty(s3Endpoint) && !s3Endpoint.StartsWith("${")
            ? "s3" : "local";
    }

    public async Task<string> UploadAsync(Stream stream, string path, CancellationToken ct = default)
    {
        var fullPath = $"{_prefix}://{path}";

        // SharpGrip WriteFileAsync expects a Stream
        if (stream.CanSeek)
            stream.Position = 0;

        await _fileSystem.WriteFileAsync(fullPath, stream);
        _logger.LogInformation("File uploaded: {Path}", path);
        return path;
    }

    public async Task<Stream> ReadAsync(string path, CancellationToken ct = default)
    {
        var fullPath = $"{_prefix}://{path}";
        var file = await _fileSystem.GetFileAsync(fullPath, ct);
        return new MemoryStream(await _fileSystem.ReadFileAsync(fullPath, ct));
    }

    public async Task DeleteAsync(string path, CancellationToken ct = default)
    {
        try
        {
            var fullPath = $"{_prefix}://{path}";
            await _fileSystem.DeleteFileAsync(fullPath, ct);
            _logger.LogInformation("File deleted: {Path}", path);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete file: {Path}", path);
        }
    }

    public async Task<bool> ExistsAsync(string path, CancellationToken ct = default)
    {
        try
        {
            var fullPath = $"{_prefix}://{path}";
            var file = await _fileSystem.GetFileAsync(fullPath, ct);
            return file != null;
        }
        catch
        {
            return false;
        }
    }

    public string GetPublicUrl(string path)
    {
        if (_prefix == "s3")
        {
            var publicUrl = _configuration["Storage:S3:PublicUrl"];
            if (!string.IsNullOrEmpty(publicUrl) && !publicUrl.StartsWith("${"))
                return $"{publicUrl.TrimEnd('/')}/{path}";
        }

        return $"/uploads/{path}";
    }
}
