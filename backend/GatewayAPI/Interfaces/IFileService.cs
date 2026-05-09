namespace GatewayAPI.Interfaces
{
    public interface IFileService
    {
        Task<string?> UploadProfileImageAsync(IFormFile? file);
        Task<string?> UploadAttachmentAsync(IFormFile? file);
        bool DeleteImage(string relativePath);
    }
}
