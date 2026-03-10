using Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using System.Text;

namespace Tests
{
    public class FileServiceTests : IDisposable
    {
        private readonly string _testStoragePath;
        private readonly GcDbContext _context;
        private readonly FileService _service;
        private readonly Guid _userId;

        public FileServiceTests()
        {
            _testStoragePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testStoragePath);

            var options = new DbContextOptionsBuilder<GcDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new GcDbContext(options);
            _userId = Guid.NewGuid();

            var config = Options.Create(new FileStorageOptions { Path = _testStoragePath });
            _service = new FileService(_context, config);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testStoragePath))
                Directory.Delete(_testStoragePath, true);
        }

        [Fact]
        public async Task UploadFileAsync_ValidFile_SavesFileAndReturnsId()
        {
            // Arrange
            var fileName = "test.txt";
            var content = "Hello World";
            var fileMock = CreateMockFile(content, fileName, "text/plain");

            // Act
            var result = await _service.UploadFileAsync(fileMock.Object, _userId);

            // Assert
            Assert.NotEqual(Guid.Empty, result.Id);

            var fileRecord = await _context.UserFiles.FindAsync(result.Id);
            Assert.NotNull(fileRecord);
            Assert.Equal(fileName, fileRecord.OriginalName);
            Assert.Equal("text/plain", fileRecord.ContentType);

            var storedPath = Path.Combine(_testStoragePath, fileRecord.StoredFileName);
            Assert.True(File.Exists(storedPath));
            Assert.Equal(content, await File.ReadAllTextAsync(storedPath));
        }

        [Fact]
        public async Task UploadFileAsync_EmptyFile_ThrowsException()
        {
            // Arrange
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(0);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.UploadFileAsync(fileMock.Object, _userId));
        }

        [Fact]
        public async Task GetFileAsync_ExistingFile_ReturnsStream()
        {
            // Arrange
            var fileName = "test.txt";
            var content = "Hello World";
            var fileMock = CreateMockFile(content, fileName, "text/plain");
            var uploadResult = await _service.UploadFileAsync(fileMock.Object, _userId);

            // Act
            var file = await _service.GetFileAsync(uploadResult.Id);

            // Assert
            Assert.NotNull(file);
            Assert.Equal("text/plain", file.Value.ContentType);
            Assert.Equal(fileName, file.Value.FileName);

            using var reader = new StreamReader(file.Value.FileStream);
            var fileContent = await reader.ReadToEndAsync();
            Assert.Equal(content, fileContent);
        }

        [Fact]
        public async Task GetFileAsync_NonExistingFile_ReturnsNull()
        {
            // Act
            var file = await _service.GetFileAsync(Guid.NewGuid());

            // Assert
            Assert.Null(file);
        }

        private Mock<IFormFile> CreateMockFile(string content, string fileName, string contentType)
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.OpenReadStream()).Returns(stream);
            fileMock.Setup(f => f.FileName).Returns(fileName);
            fileMock.Setup(f => f.ContentType).Returns(contentType);
            fileMock.Setup(f => f.Length).Returns(stream.Length);
            fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns((Stream target, CancellationToken _) => stream.CopyToAsync(target));
            return fileMock;
        }
    }
}