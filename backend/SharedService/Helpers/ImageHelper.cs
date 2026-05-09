using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace SharedService.Utils
{
    public static class ImageHelper
    {
        public static string SaveFile(IFormFile file, string rootPath, string subFolder)
        {
            if (file == null || file.Length == 0) return string.Empty;
            // Path logic: wwwroot/{subFolder}/yyyy/MM
            var relativeFolder = Path.Combine(subFolder, DateTime.Now.ToString("yyyy"), DateTime.Now.ToString("MM"));
            var physicalFolder = Path.Combine(rootPath, "wwwroot", relativeFolder);

            if (!Directory.Exists(physicalFolder))
                Directory.CreateDirectory(physicalFolder);

            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrEmpty(extension) && file.ContentType == "image/jpeg") extension = ".jpg";

            var fileName = $"{Guid.NewGuid():N}{extension}";
            var fullPath = Path.Combine(physicalFolder, fileName);

            //var uploads = Path.Combine(rootPath, "wwwroot", "uploads");
            //if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);
            //var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            //var path = Path.Combine(uploads, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                file.CopyTo(stream);
            }

            // Return URL: /attachments/2026/03/filename.jpg
            return $"/{relativeFolder.Replace("\\", "/")}/{fileName}";
        }

        public static bool DeleteImage(string relativePath, string rootPath)
        {
            if (string.IsNullOrEmpty(relativePath)) return false;
            var full = Path.Combine(rootPath, "wwwroot", relativePath.TrimStart('/', '\\'));
            if (File.Exists(full))
            {
                File.Delete(full);
                return true;
            }
            return false;
        }
    }
}
