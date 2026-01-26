using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using PaperlessModels.DTOs;
using PaperlessModels.Models;
using PaperlessREST.Controllers;
using PaperlessREST.Exceptions;
using PaperlessREST.Repositories;
using System.Security.Claims;
using System.Text;

namespace PaperlessREST.Tests.Unit;

[TestFixture]
public class DocumentControllerMockTests
{
    private DocumentController _controller;
    private Mock<IDocumentRepository> _repositoryMock;
    private Mock<ILogger<DocumentController>> _loggerMock;

    [SetUp]
    public void Setup()
    {
        _repositoryMock = new Mock<IDocumentRepository>();
        _loggerMock = new Mock<ILogger<DocumentController>>();

        _controller = new DocumentController(_repositoryMock.Object, _loggerMock.Object);

        // Fake authenticated user
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1")
        }, "TestAuth"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    private static IFormFile CreateFakeFile(string name = "test.pdf", string content = "pdf")
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);

        return new FormFile(stream, 0, bytes.Length, "file", name)
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        };
    }


    #region GetAllDocuments

    [Test]
    public async Task GetAllDocuments_ReturnsOk()
    {
        _repositoryMock.Setup(r => r.GetAllDocumentsAsync(1))
            .ReturnsAsync(new List<OwnDocumentDto>());

        var result = await _controller.GetAllDocuments();

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetAllDocuments_NoUser_ReturnsUnauthorized()
    {
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal();

        var result = await _controller.GetAllDocuments();

        Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
    }

    #endregion

    #region GetDocumentById

    [Test]
    public async Task GetDocumentById_InvalidId_ReturnsBadRequest()
    {
        var result = await _controller.GetDocumentById(0);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task GetDocumentById_NoUser_ReturnsUnauthorized()
    {
        _repositoryMock.Setup(r => r.GetDocumentByIdAsync(1, 1))
            .ReturnsAsync(new DocumentDto { FileName = "test.pdf" });

        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal();

        var result = await _controller.GetDocumentById(1);

        Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
    }

    [Test]
    public async Task GetDocumentById_Forbidden_ReturnsForbid()
    {
        _repositoryMock.Setup(r => r.GetDocumentByIdAsync(1, 1))
            .ThrowsAsync(new ForbiddenContentException("Document", 1, 1));

        var result = await _controller.GetDocumentById(1);

        Assert.That(result, Is.InstanceOf<ForbidResult>());
    }

    [Test]
    public async Task GetDocumentById_WrongDocument_ReturnsNotFound()
    {
        _repositoryMock.Setup(r => r.GetDocumentByIdAsync(1, 1))
            .ThrowsAsync(new NotFoundException("Document", 1));

        var result = await _controller.GetDocumentById(1);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task GetDocumentById_Valid_ReturnsOk()
    {
        _repositoryMock.Setup(r => r.GetDocumentByIdAsync(1, 1))
            .ReturnsAsync(new DocumentDto { FileName = "test.pdf" });

        var result = await _controller.GetDocumentById(1);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    #endregion

    #region UploadDocument

    [Test]
    public async Task UploadDocument_NullFile_ReturnsBadRequest()
    {
        var result = await _controller.UploadDocument(null);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task UploadDocument_FileTooLarge_ReturnsBadRequest()
    {
        var largeContent = new byte[6 * 1024 * 1024]; // 6 MB
        var stream = new MemoryStream(largeContent);

        var file = new FormFile(stream, 0, largeContent.Length, "file", "large.pdf")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        };

        var result = await _controller.UploadDocument(file);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task UploadDocument_NoUser_ReturnsUnauthorized()
    {
        var file = CreateFakeFile();

        _repositoryMock.Setup(r => r.UploadDocumentAsync(file, 1))
            .ReturnsAsync(new Document { Id = 10 });

        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal();

        var result = await _controller.UploadDocument(file);

        Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
    }

    [Test]
    public async Task UploadDocument_FileExists_ReturnsConflict()
    {
        var file = CreateFakeFile();

        _repositoryMock.Setup(r => r.UploadDocumentAsync(file, 1))
            .ThrowsAsync(new FileAlreadyExistsException(file.FileName));

        var result = await _controller.UploadDocument(file);

        Assert.That(result, Is.InstanceOf<ConflictObjectResult>());
    }

    [Test]
    public async Task UploadDocument_Success_ReturnsCreated()
    {
        var file = CreateFakeFile();

        _repositoryMock.Setup(r => r.UploadDocumentAsync(file, 1))
            .ReturnsAsync(new Document { Id = 10 });

        var result = await _controller.UploadDocument(file);

        Assert.That(result, Is.InstanceOf<CreatedAtActionResult>());
    }

    #endregion

    #region DeleteDocument

    [Test]
    public async Task DeleteDocument_InvalidId_ReturnsBadRequest()
    {
        var result = await _controller.DeleteDocument(0);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task DeleteDocument_NoUser_ReturnsUnauthorized()
    {
        _repositoryMock.Setup(r => r.DeleteDocumentAsync(1, 1))
            .Returns(Task.CompletedTask);

        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal();

        var result = await _controller.DeleteDocument(1);

        Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
    }

    [Test]
    public async Task DeleteDocument_Forbidden_ReturnsForbid()
    {
        _repositoryMock.Setup(r => r.DeleteDocumentAsync(1, 1))
            .ThrowsAsync(new ForbiddenActionException("Document", 1, 1));

        var result = await _controller.DeleteDocument(1);

        Assert.That(result, Is.InstanceOf<ForbidResult>());
    }

    [Test]
    public async Task DeleteDocument_Success_ReturnsNoContent()
    {
        _repositoryMock.Setup(r => r.DeleteDocumentAsync(1, 1))
            .Returns(Task.CompletedTask);

        var result = await _controller.DeleteDocument(1);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    #endregion

    #region UpdateDocument

    [Test]
    public async Task UpdateDocument_Success_ReturnsOk()
    {
        var dto = new DocumentDto { FileName = "updated.pdf" };

        _repositoryMock.Setup(r => r.UpdateDocumentAsync(1, dto, 1))
            .ReturnsAsync(dto);

        var result = await _controller.UpdateDocument(1, dto);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    #endregion

}
