using GatewayAPI.Interfaces;
using SharedService.Utils;

namespace GatewayAPI.Services
{
    public class FileService : IFileService
    {
        private readonly IWebHostEnvironment _env;
        public FileService(IWebHostEnvironment env) => _env = env;

        public Task<string?> UploadProfileImageAsync(IFormFile? file)
        {
            return UploadFileAsync(file, "uploads");
        }

        public Task<string?> UploadAttachmentAsync(IFormFile? file)
        {
            return UploadFileAsync(file, "attachments");
        }

        private Task<string?> UploadFileAsync(IFormFile? file, string subFolder)
        {
            if (file == null || file.Length == 0)
                return Task.FromResult<string?>(null);

            // Using the refactored ImageHelper that accepts a subFolder
            var path = ImageHelper.SaveFile(file, _env.ContentRootPath, subFolder);
            return Task.FromResult<string?>(path);
        }
        public bool DeleteImage(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return false;
            var full = Path.Combine(_env.ContentRootPath, "wwwroot", relativePath.TrimStart('/', '\\'));
            if (File.Exists(full))
            {
                File.Delete(full);
                return true;
            }
            return false;
        }
    }
}
