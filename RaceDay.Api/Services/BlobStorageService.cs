using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.Api.Services;

public interface IBlobStorageService
{
    Task<string> UploadAsync(string containerName, IFormFile file);
    Task DeleteAsync(string containerName, string blobUrl);
}

public class BlobStorageService : IBlobStorageService
{
    private static readonly string[] AllowedTypes = { "image/jpeg", "image/png", "image/webp" };
    private const long MaxBytes = 5 * 1024 * 1024; // 5 MB

    private readonly BlobServiceClient _client;

    public BlobStorageService(IConfiguration config)
    {
        var connectionString = config["AzureStorage:ConnectionString"]
            ?? throw new InvalidOperationException("AzureStorage:ConnectionString is not configured.");
        _client = new BlobServiceClient(connectionString);
    }

    public async Task<string> UploadAsync(string containerName, IFormFile file)
    {
        if (file.Length == 0 || file.Length > MaxBytes)
            throw new ArgumentException("File must be between 1 byte and 5 MB.");

        if (!AllowedTypes.Contains(file.ContentType))
            throw new ArgumentException("Only JPEG, PNG or WebP images are allowed.");

        var container = _client.GetBlobContainerClient(containerName);
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var blob = container.GetBlobClient($"{Guid.NewGuid()}{extension}");

        await using var stream = file.OpenReadStream();
        await blob.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType });

        return blob.Uri.ToString();
    }

    public async Task DeleteAsync(string containerName, string blobUrl)
    {
        var blobName = Path.GetFileName(new Uri(blobUrl).LocalPath);
        var container = _client.GetBlobContainerClient(containerName);
        await container.DeleteBlobIfExistsAsync(blobName);
    }
}