// Controllers/FilesController.cs
using Application.Services.Interfaces;
using Common;
using GoogleClass.DTOs.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoogleClassroom.Controllers
{
    [ApiController]
    [Route("api/files")]
    public class FilesController : ControllerBase
    {
        private readonly IFileService _fileService;

        public FilesController(IFileService fileService)
        {
            _fileService = fileService;
        }

        /// <summary>
        /// Загрузить файл
        /// </summary>
        [HttpPost("upload")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        [ProducesResponseType(typeof(ApiResponse<IdRequestDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            var userId = HttpContext.GetUserId()!.Value;
            var result = await _fileService.UploadFileAsync(file, userId);
            return Ok(new ApiResponse<IdRequestDto>
            {
                Type = ApiResponseType.Success,
                Message = null,
                Data = result
            });
        }

        /// <summary>
        /// Получить файл по ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> Download(Guid id)
        {
            var fileInfo = await _fileService.GetFileAsync(id);
            if (fileInfo == null)
                return NotFound(new ApiResponse<object>
                {
                    Type = ApiResponseType.Error,
                    Message = "File not found",
                    Data = null
                });

            return File(fileInfo.Value.FileStream, fileInfo.Value.ContentType, fileInfo.Value.FileName);
        }
    }
}
