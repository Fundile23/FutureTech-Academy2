using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using FutureTechAcademy.Services.Interfaces;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;

namespace FutureTechAcademy.Services
{
    public class BlobStorageService : IBlobStorageService
    {
        private readonly BlobContainerClient _containerClient;
        private readonly ILogger<BlobStorageService> _logger;
        private readonly string _connectionString = string.Empty;

        public BlobStorageService(IConfiguration config, ILogger<BlobStorageService> logger)
        {
            _logger = logger;
            _connectionString = config["Azure:BlobStorage:ConnectionString"];
            var containerName = config["Azure:BlobStorage:ContainerName"] ?? "student-images";

            var blobServiceClient = new BlobServiceClient(_connectionString);
            _containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            _containerClient.CreateIfNotExists();
            _containerClient.SetAccessPolicy(PublicAccessType.None);

            _logger.LogInformation($"Blob storage initialized with container: {containerName}");
        }

        private async Task<byte[]> ResizeImageAsync(IFormFile file, int maxWidth = 300, int maxHeight = 300)
        {
            using var originalStream = file.OpenReadStream();
            using var image = await Image.LoadAsync(originalStream);

            var ratio = Math.Min((double)maxWidth / image.Width, (double)maxHeight / image.Height);
            var newWidth = (int)(image.Width * ratio);
            var newHeight = (int)(image.Height * ratio);

            image.Mutate(x => x.Resize(newWidth, newHeight));

            var outputStream = new MemoryStream();

            if (file.ContentType == "image/jpeg")
            {
                await image.SaveAsync(outputStream, new JpegEncoder());
            }
            else
            {
                await image.SaveAsync(outputStream, new PngEncoder());
            }

            return outputStream.ToArray();
        }

        private void ValidateFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty");

            if (file.Length > 5 * 1024 * 1024)
                throw new ArgumentException("File size exceeds 5MB limit");

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
                throw new ArgumentException("Only JPG, JPEG, and PNG files are allowed");

            var allowedTypes = new[] { "image/jpeg", "image/png" };
            if (!allowedTypes.Contains(file.ContentType.ToLowerInvariant()))
                throw new ArgumentException("Invalid image format");
        }

        public async Task<string> UploadImageAsync(IFormFile file, string studentId)
        {
            try
            {
                ValidateFile(file);

                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                var fileName = $"{studentId}-{Guid.NewGuid()}{extension}";
                var blobClient = _containerClient.GetBlobClient(fileName);

                // Resize image before upload
                var resizedImageBytes = await ResizeImageAsync(file);

                using var stream = new MemoryStream(resizedImageBytes);
                await blobClient.UploadAsync(stream, new BlobHttpHeaders
                {
                    ContentType = file.ContentType
                });

                _logger.LogInformation($"Image {fileName} uploaded for student {studentId}");

                return GenerateSasToken(fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error uploading image for student {studentId}");
                throw;
            }
        }

        public string GenerateSasToken(string blobName, int expiryHours = 1)
        {
            try
            {
                var blobClient = _containerClient.GetBlobClient(blobName);

                if (!blobClient.Exists())
                {
                    _logger.LogWarning($"Blob {blobName} does not exist");
                    return null;
                }

                var sasBuilder = new BlobSasBuilder
                {
                    BlobContainerName = _containerClient.Name,
                    BlobName = blobName,
                    Resource = "b",
                    ExpiresOn = DateTimeOffset.UtcNow.AddHours(expiryHours)
                };

                sasBuilder.SetPermissions(BlobSasPermissions.Read);

                var sasUri = blobClient.GenerateSasUri(sasBuilder);

                _logger.LogInformation($"Generated SAS token for {blobName} expiring in {expiryHours} hours");

                return sasUri.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating SAS token for {blobName}");
                throw;
            }
        }

        public async Task<bool> DeleteImageAsync(string imageUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(imageUrl))
                    return false;

                var uri = new Uri(imageUrl);
                var blobName = Path.GetFileName(uri.LocalPath);

                var blobClient = _containerClient.GetBlobClient(blobName);

                if (await blobClient.ExistsAsync())
                {
                    await blobClient.DeleteAsync();
                    _logger.LogInformation($"Deleted image {blobName}");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting image {imageUrl}");
                throw;
            }
        }
    }
}