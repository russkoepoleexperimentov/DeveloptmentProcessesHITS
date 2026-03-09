using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoogleClass.DTOs.Common;
using Microsoft.AspNetCore.Http;

namespace Application.Services.Interfaces
{
    public interface IFileService
    {
        Task<IdRequestDto> UploadFileAsync(IFormFile file, Guid userId);
        Task<(Stream FileStream, string ContentType, string FileName)?> GetFileAsync(Guid fileId);
    }
}
