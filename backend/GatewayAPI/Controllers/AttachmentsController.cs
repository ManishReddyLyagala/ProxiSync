using GatewayAPI.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GatewayAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AttachmentsController : Controller
    {
        private readonly IFileService _fileService;

        public AttachmentsController(IFileService fileService)
        {
            _fileService = fileService;
        }

        [HttpPost("upload")]
        [RequestSizeLimit(5 * 1024 * 1024)] // 5MB Limit for chat files
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file selected." });

            // This calls our new logic that saves to the /attachments folder
            var relativeUrl = await _fileService.UploadAttachmentAsync(file);

            if (string.IsNullOrEmpty(relativeUrl))
                return StatusCode(500, new { message = "Failed to upload attachment." });

            // Construct the full URL so the Chat Service doesn't have to guess the Gateway's host
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var fullUrl = $"{baseUrl}{relativeUrl}";

            return Ok(new
            {
                url = fullUrl,
                fileName = file.FileName,
                contentType = file.ContentType
            });
        }
    }


}
