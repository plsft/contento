namespace Contento.Core.Interfaces;

public interface IFileStorageService
{
    Task<string> UploadAsync(Stream stream, string path, CancellationToken ct = default);
    Task<Stream> ReadAsync(string path, CancellationToken ct = default);
    Task DeleteAsync(string path, CancellationToken ct = default);
    Task<bool> ExistsAsync(string path, CancellationToken ct = default);
    string GetPublicUrl(string path);
}
