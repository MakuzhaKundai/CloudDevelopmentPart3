using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EventEase.Services
{
    public class BlobStorageService : IBlobStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly BlobContainerClient _containerClient;
        private readonly ILogger<BlobStorageService> _logger;
        private readonly string _containerName;

        public BlobStorageService(
            IConfiguration configuration,
            ILogger<BlobStorageService> logger)
        {
            var connectionString = configuration.GetConnectionString("AzureStorage") ??
                throw new ArgumentNullException("AzureStorage", "Connection string is not configured");

            _containerName = configuration["AzureBlobStorage:ContainerName"] ?? "event-images";
            _logger = logger;

            try
            {
                _blobServiceClient = new BlobServiceClient(connectionString);
                _containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Blob Storage client");
                throw;
            }
        }

        public async Task InitializeAsync()
        {
            try
            {
                await _containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);
                _logger.LogInformation("Blob container initialized: {ContainerName}", _containerName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing blob container");
                throw;
            }
        }

        public async Task<string> UploadImageToBlob(IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                _logger.LogWarning("Upload attempted with empty file");
                throw new ArgumentException("Image file is required");
            }

            try
            {
                var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(imageFile.FileName)}";
                var blobClient = _containerClient.GetBlobClient(uniqueFileName);

                using (var stream = imageFile.OpenReadStream())
                {
                    await blobClient.UploadAsync(stream, new BlobUploadOptions
                    {
                        HttpHeaders = new BlobHttpHeaders
                        {
                            ContentType = imageFile.ContentType,
                            CacheControl = "public, max-age=31536000"
                        }
                    });
                }

                return blobClient.Uri.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading image");
                throw;
            }
        }

        public async Task<bool> DeleteImageFromBlob(string? imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl))
            {
                _logger.LogWarning("Delete attempted with empty URL");
                return false;
            }

            try
            {
                var blobName = Path.GetFileName(new Uri(imageUrl).LocalPath);
                var blobClient = _containerClient.GetBlobClient(blobName);
                var response = await blobClient.DeleteIfExistsAsync();
                return response.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting image");
                throw;
            }
        }

        public async Task<Stream> DownloadBlobAsync(string blobName)
        {
            if (string.IsNullOrEmpty(blobName))
            {
                throw new ArgumentException("Blob name is required");
            }

            try
            {
                var blobClient = _containerClient.GetBlobClient(blobName);
                var memoryStream = new MemoryStream();
                await blobClient.DownloadToAsync(memoryStream);
                memoryStream.Position = 0;
                return memoryStream;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading blob");
                throw;
            }
        }

        public async Task<string?> GenerateSasTokenAsync(string blobName, int expiryMinutes = 60)
        {
            if (string.IsNullOrEmpty(blobName))
            {
                return null;
            }

            try
            {
                var blobClient = _containerClient.GetBlobClient(blobName);
                if (!await blobClient.ExistsAsync())
                {
                    return null;
                }

                var sasBuilder = new BlobSasBuilder
                {
                    BlobContainerName = _containerClient.Name,
                    BlobName = blobName,
                    Resource = "b",
                    ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(expiryMinutes)
                };

                sasBuilder.SetPermissions(BlobSasPermissions.Read);
                return $"{blobClient.Uri}{blobClient.GenerateSasUri(sasBuilder).Query}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating SAS token");
                throw;
            }
        }

        public Task<bool> BlobExistsAsync(string blobName)
        {
            throw new NotImplementedException();
        }

        public Task SetBlobMetadataAsync(string blobName, IDictionary<string, string> metadata)
        {
            throw new NotImplementedException();
        }
    }
}