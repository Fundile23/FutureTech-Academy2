using Microsoft.AspNetCore.Http;

namespace FutureTechAcademy.Services.Interfaces
{
    public interface IBlobStorageService
    {
        // Upload profile image and return SAS URL
        Task<string> UploadImageAsync(IFormFile file, string studentId);

        // Delete image when student is deleted
        Task<bool> DeleteImageAsync(string imageUrl);

        // Generate SAS token for existing image
        string GenerateSasToken(string blobName, int expiryHours = 1);
    }
}