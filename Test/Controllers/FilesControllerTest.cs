using Application.Services.Interfaces;
using Common;
using GoogleClass.DTOs.Common;
using GoogleClassroom.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Xunit;
using Microsoft.AspNetCore.Authorization;

namespace Tests.Controllers;

public class FilesControllerTests
{
    private readonly Mock<IFileService> _fileServiceMock;
    private readonly FilesController _controller;
    private readonly Mock<HttpContext> _httpContextMock;
    private readonly Guid _userId;
    private readonly Guid _fileId;

    public FilesControllerTests()
    {
        _fileServiceMock = new Mock<IFileService>();
        _httpContextMock = new Mock<HttpContext>();
        _userId = Guid.NewGuid();
        _fileId = Guid.NewGuid();

        var claims = new List<Claim>
        {
            new Claim("userId", _userId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, _userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _httpContextMock.Setup(x => x.User).Returns(principal);

        _controller = new FilesController(_fileServiceMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = _httpContextMock.Object
            }
        };
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

    [Fact]
    public async Task Upload_ValidFile_ShouldReturnOkWithIdRequestDto()
    {
        // Arrange
        var fileName = "test.txt";
        var content = "Hello World";
        var fileMock = CreateMockFile(content, fileName, "text/plain");

        var expectedResult = new IdRequestDto { Id = _fileId };

        _fileServiceMock
            .Setup(x => x.UploadFileAsync(fileMock.Object, _userId))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.Upload(fileMock.Object);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<IdRequestDto>>().Subject;

        response.Type.Should().Be(ApiResponseType.Success);
        response.Data.Should().BeEquivalentTo(expectedResult);
        response.Message.Should().BeNull();

        _fileServiceMock.Verify(x => x.UploadFileAsync(fileMock.Object, _userId), Times.Once);
    }

    [Fact]
    public async Task Download_ExistingFile_ShouldReturnFileStream()
    {
        // Arrange
        var fileName = "test.txt";
        var contentType = "text/plain";
        var content = "Hello World";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        // Создаем кортеж, который возвращает сервис
        var fileInfo = (stream, contentType, fileName);

        _fileServiceMock
            .Setup(x => x.GetFileAsync(_fileId))
            .ReturnsAsync(fileInfo);

        // Act
        var result = await _controller.Download(_fileId);

        // Assert
        var fileResult = result.Should().BeOfType<FileStreamResult>().Subject;
        fileResult.ContentType.Should().Be(contentType);
        fileResult.FileDownloadName.Should().Be(fileName);

        _fileServiceMock.Verify(x => x.GetFileAsync(_fileId), Times.Once);
    }

    [Fact]
    public async Task Download_NonExistingFile_ShouldReturnNotFoundWithErrorResponse()
    {
        // Arrange
        _fileServiceMock
            .Setup(x => x.GetFileAsync(_fileId))
            .ReturnsAsync((System.ValueTuple<System.IO.Stream, string, string>?)null);

        // Act
        var result = await _controller.Download(_fileId);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var response = notFoundResult.Value.Should().BeOfType<ApiResponse<object>>().Subject;

        response.Type.Should().Be(ApiResponseType.Error);
        response.Message.Should().Be("File not found");
        response.Data.Should().BeNull();

        _fileServiceMock.Verify(x => x.GetFileAsync(_fileId), Times.Once);
    }

    [Fact]
    public async Task Upload_WhenServiceThrows_ShouldPropagateException()
    {
        // Arrange
        var fileName = "test.txt";
        var content = "Hello World";
        var fileMock = CreateMockFile(content, fileName, "text/plain");

        var expectedException = new Exception("Service error");

        _fileServiceMock
            .Setup(x => x.UploadFileAsync(fileMock.Object, _userId))
            .ThrowsAsync(expectedException);

        // Act
        Func<Task> act = async () => await _controller.Upload(fileMock.Object);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("Service error");
    }

    [Fact]
    public async Task Upload_EmptyFile_ShouldPropagateArgumentException()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(0);

        var expectedException = new ArgumentException("File is empty");

        _fileServiceMock
            .Setup(x => x.UploadFileAsync(fileMock.Object, _userId))
            .ThrowsAsync(expectedException);

        // Act
        Func<Task> act = async () => await _controller.Upload(fileMock.Object);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("File is empty");
    }

    [Fact]
    public void Upload_ShouldHaveAuthorizeAttribute()
    {
        // Act
        var methodInfo = typeof(FilesController).GetMethod(nameof(FilesController.Upload));
        var attributes = methodInfo?.GetCustomAttributes(typeof(AuthorizeAttribute), false);

        // Assert
        attributes.Should().NotBeNull();
        attributes.Should().HaveCount(1);
        var authorizeAttribute = attributes?.First() as AuthorizeAttribute;
        authorizeAttribute?.AuthenticationSchemes.Should().Be("Bearer");
    }

    [Fact]
    public async Task Download_WithLargeFile_ShouldReturnFileStream()
    {
        // Arrange
        var fileName = "large.bin";
        var contentType = "application/octet-stream";
        var content = new string('A', 1024 * 1024); // 1MB файл
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        var fileInfo = (stream, contentType, fileName);

        _fileServiceMock
            .Setup(x => x.GetFileAsync(_fileId))
            .ReturnsAsync(fileInfo);

        // Act
        var result = await _controller.Download(_fileId);

        // Assert
        var fileResult = result.Should().BeOfType<FileStreamResult>().Subject;
        fileResult.ContentType.Should().Be(contentType);
        fileResult.FileDownloadName.Should().Be(fileName);

        _fileServiceMock.Verify(x => x.GetFileAsync(_fileId), Times.Once);
    }

    [Fact]
    public async Task Upload_WithDifferentFileTypes_ShouldReturnOk()
    {
        // Arrange
        var testCases = new[]
        {
            new { FileName = "test.pdf", ContentType = "application/pdf", Content = "PDF content" },
            new { FileName = "image.jpg", ContentType = "image/jpeg", Content = "Image content" },
            new { FileName = "doc.docx", ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document", Content = "Word content" }
        };

        foreach (var testCase in testCases)
        {
            var fileMock = CreateMockFile(testCase.Content, testCase.FileName, testCase.ContentType);
            var expectedResult = new IdRequestDto { Id = Guid.NewGuid() };

            _fileServiceMock
                .Setup(x => x.UploadFileAsync(fileMock.Object, _userId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.Upload(fileMock.Object);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<ApiResponse<IdRequestDto>>().Subject;

            response.Type.Should().Be(ApiResponseType.Success);
            response.Data.Should().BeEquivalentTo(expectedResult);

            _fileServiceMock.Verify(x => x.UploadFileAsync(fileMock.Object, _userId), Times.Once);

            // Сбрасываем счетчик для следующей итерации
            _fileServiceMock.Invocations.Clear();
        }
    }

    [Fact]
    public void Download_ShouldNotHaveAuthorizeAttribute()
    {
        // Act
        var methodInfo = typeof(FilesController).GetMethod(nameof(FilesController.Download));
        var attributes = methodInfo?.GetCustomAttributes(typeof(AuthorizeAttribute), false);

        // Assert
        attributes.Should().BeNullOrEmpty();
    }
}