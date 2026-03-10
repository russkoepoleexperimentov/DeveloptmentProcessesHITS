
using Application.Services.Interfaces;
using Domain.Models;
using GoogleClass.DTOs.Common;
using GoogleClass.Models;
using Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class FileStorageOptions
    {
        public string Path { get; set; } = "App_Data/uploads";
    }

    public class FileService : IFileService
    {
        private readonly GcDbContext _context;
        private readonly string _storagePath;

        public FileService(GcDbContext context, IOptions<FileStorageOptions> options)
        {
            _context = context;
            _storagePath = options.Value.Path;
            if (!Directory.Exists(_storagePath))
                Directory.CreateDirectory(_storagePath);
        }

        public async Task<IdRequestDto> UploadFileAsync(IFormFile file, Guid userId)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty");

            var originalName = file.FileName;
            var contentType = file.ContentType;
            var size = file.Length;

            // Генерируем уникальное имя для хранения
            var extension = Path.GetExtension(originalName);
            var storedName = $"{Guid.NewGuid():N}{extension}";
            var storedPath = Path.Combine(_storagePath, storedName);

            // Сохраняем файл на диск
            using (var stream = new FileStream(storedPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Создаем запись в БД
            var appFile = new UserFile
            {
                Id = Guid.NewGuid(),
                OriginalName = originalName,
                StoredFileName = storedName,
                ContentType = contentType,
                UserId = userId,
            };

            _context.UserFiles.Add(appFile);
            await _context.SaveChangesAsync();

            return new IdRequestDto { Id = appFile.Id };
        }

        public async Task<(Stream FileStream, string ContentType, string FileName)?> GetFileAsync(Guid fileId)
        {
            var file = await _context.UserFiles.FirstOrDefaultAsync(f => f.Id == fileId);
            if (file == null)
                return null;

            var filePath = Path.Combine(_storagePath, file.StoredFileName);
            if (!File.Exists(filePath))
                return null;

            var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return (stream, file.ContentType, file.OriginalName);
        }
    }
}