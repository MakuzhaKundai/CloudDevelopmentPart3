using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace EventEase.Services
{
    public interface IBlobStorageService
    {
        Task InitializeAsync();
        Task<string> UploadImageToBlob(IFormFile imageFile);
        Task<bool> DeleteImageFromBlob(string? imageUrl);
        Task<Stream> DownloadBlobAsync(string blobName);
        Task<string?> GenerateSasTokenAsync(string blobName, int expiryMinutes = 60);
    }
}