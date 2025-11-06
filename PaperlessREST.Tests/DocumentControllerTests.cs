using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using AutoMapper;
using PaperlessREST.Data;
using PaperlessREST.Services;
using PaperlessREST.Exceptions;
using PaperlessModels.Models;
using PaperlessModels.DTOs;

namespace PaperlessREST.Tests;

[TestFixture]
public class DocumentControllerTests
{
    private DocumentController _controller;
    private ApplicationDBContext _context;
    private Mock<IMapper> _mockMapper;
    private Mock<IMessageQueueService> _mockQueueService;
    private Mock<ILogger<DocumentController>> _mockLogger;

    [SetUp]
    public void Setup()
    {
        // Setup in-memory database with unique name for each test
        var options = new DbContextOptionsBuilder<ApplicationDBContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDBContext(options);

        // Setup mocks
        _mockMapper = new Mock<IMapper>();
        _mockQueueService = new Mock<IMessageQueueService>();
        _mockLogger = new Mock<ILogger<DocumentController>>();

        // Create controller with mocked dependencies
        _controller = new DocumentController(_context, _mockMapper.Object, _mockQueueService.Object, _mockLogger.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region GetAllDocuments Tests

    [Test]
    public void GetAllDocuments_WhenDocumentsExist_ReturnsOkWithDocuments()
    {

        _context.Documents.AddRange(
            new Document { Id = 1, Name = "Document1.pdf", Filetype = "pdf" },
            new Document { Id = 2, Name = "Document2.docx", Filetype = "docx" }
        );
        _context.SaveChanges();


        var result = _controller.GetAllDocuments() as OkObjectResult;


        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        var documents = result.Value as List<Document>;
        Assert.That(documents, Is.Not.Null);
        Assert.That(documents.Count, Is.EqualTo(2));
        Assert.That(documents[0].Name, Is.EqualTo("Document1.pdf"));
    }

    [Test]
    public void GetAllDocuments_WhenNoDocuments_ReturnsOkWithEmptyList()
    {

        var result = _controller.GetAllDocuments() as OkObjectResult;


        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        var documents = result.Value as List<Document>;
        Assert.That(documents, Is.Not.Null);
        Assert.That(documents.Count, Is.EqualTo(0));
    }


    [Test]
    public void GetDocumentById_WhenDocumentExists_ReturnsOkWithDocument()
    {

        var document = new Document { Id = 1, Name = "TestDoc.pdf", Filetype = "pdf" };
        _context.Documents.Add(document);
        _context.SaveChanges();


        var result = _controller.GetDocumentById(1) as OkObjectResult;


        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        var returnedDoc = result.Value as Document;
        Assert.That(returnedDoc, Is.Not.Null);
        Assert.That(returnedDoc.Id, Is.EqualTo(1));
        Assert.That(returnedDoc.Name, Is.EqualTo("TestDoc.pdf"));
    }

    [Test]
    public void GetDocumentById_WhenDocumentDoesNotExist_ReturnsNotFound()
    {

        var result = _controller.GetDocumentById(999);


        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public void GetDocumentById_WhenIdIsZero_ThrowsInvalidIdException()
    {

        Assert.Throws<InvalidIdException>(() => _controller.GetDocumentById(0));
    }

    [Test]
    public void GetDocumentById_WhenIdIsNegative_ThrowsInvalidIdException()
    {

        Assert.Throws<InvalidIdException>(() => _controller.GetDocumentById(-1));
    }


    [Test]
    public async Task UploadDocument_WithValidDocument_ReturnsCreatedAtAction()
    {

        var docDto = new DocumentDto
        {
            Name = "NewDocument.pdf",
            Filetype = "pdf",
            ByteSize = 1024,
            CreatedAt = DateTime.UtcNow
        };
        var docModel = new Document
        {
            Id = 1,
            Name = "NewDocument.pdf",
            Filetype = "pdf",
            ByteSize = 1024,
            CreatedAt = DateTime.UtcNow
        };

        _mockMapper.Setup(m => m.Map<Document>(docDto)).Returns(docModel);
        _mockMapper.Setup(m => m.Map<DocumentDto>(It.IsAny<Document>())).Returns(docDto);
        _mockQueueService.Setup(q => q.PublishAsync("ocr_queue", It.IsAny<Document>())).Returns(Task.CompletedTask);


        var result = await _controller.UploadDocument(docDto) as CreatedAtActionResult;


        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(201));
        Assert.That(result.ActionName, Is.EqualTo(nameof(DocumentController.GetDocumentById)));


        var savedDoc = _context.Documents.FirstOrDefault();
        Assert.That(savedDoc, Is.Not.Null);
        Assert.That(savedDoc.Name, Is.EqualTo("NewDocument.pdf"));


        _mockQueueService.Verify(q => q.PublishAsync("ocr_queue", It.IsAny<Document>()), Times.Once);
    }

    [Test]
    public async Task UploadDocument_PublishesToQueue_WithCorrectQueueName()
    {

        var docDto = new DocumentDto { Name = "QueueTest.pdf", Filetype = "pdf" };
        var docModel = new Document { Id = 1, Name = "QueueTest.pdf", Filetype = "pdf" };

        _mockMapper.Setup(m => m.Map<Document>(docDto)).Returns(docModel);
        _mockMapper.Setup(m => m.Map<DocumentDto>(It.IsAny<Document>())).Returns(docDto);
        _mockQueueService.Setup(q => q.PublishAsync(It.IsAny<string>(), It.IsAny<Document>())).Returns(Task.CompletedTask);


        await _controller.UploadDocument(docDto);


        _mockQueueService.Verify(q => q.PublishAsync("ocr_queue", It.IsAny<Document>()), Times.Once);
    }

    #endregion

    #region DeleteDocument Tests

    [Test]
    public void DeleteDocument_WhenDocumentExists_ReturnsNoContent()
    {

        var document = new Document { Id = 1, Name = "ToDelete.pdf", Filetype = "pdf" };
        _context.Documents.Add(document);
        _context.SaveChanges();

        var result = _controller.DeleteDocument(1);

        Assert.That(result, Is.TypeOf<NoContentResult>());
        Assert.That(_context.Documents.Find(1), Is.Null);
    }

    [Test]
    public void DeleteDocument_WhenDocumentDoesNotExist_ReturnsNotFound()
    {

        var result = _controller.DeleteDocument(999);

        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public void DeleteDocument_WhenIdIsInvalid_ThrowsInvalidIdException()
    {

        Assert.Throws<InvalidIdException>(() => _controller.DeleteDocument(0));
        Assert.Throws<InvalidIdException>(() => _controller.DeleteDocument(-5));
    }

    [Test]
    public void DeleteDocument_RemovesDocumentFromDatabase()
    {

        var document = new Document { Id = 5, Name = "RemoveMe.pdf", Filetype = "pdf" };
        _context.Documents.Add(document);
        _context.SaveChanges();

        Assert.That(_context.Documents.Find(5), Is.Not.Null); // Verify it exists first


        _controller.DeleteDocument(5);


        Assert.That(_context.Documents.Find(5), Is.Null);
        Assert.That(_context.Documents.Count(), Is.EqualTo(0));
    }

    #endregion

    #region UpdateDocument Tests

    [Test]
    public void UpdateDocument_WhenDocumentExists_ReturnsOkWithUpdatedDocument()
    {

        var originalDoc = new Document
        {
            Id = 1,
            Name = "Original.pdf",
            Filetype = "pdf",
            ByteSize = 512,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };
        _context.Documents.Add(originalDoc);
        _context.SaveChanges();

        var updatedDto = new DocumentDto
        {
            Name = "Updated.pdf",
            Filetype = "docx",
            ByteSize = 1024,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow
        };

        _mockMapper.Setup(m => m.Map<DocumentDto>(It.IsAny<Document>())).Returns(updatedDto);


        var result = _controller.UpdateDocument(1, updatedDto) as OkObjectResult;


        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));

        var updatedDoc = _context.Documents.Find(1);
        Assert.That(updatedDoc, Is.Not.Null);
        Assert.That(updatedDoc.Name, Is.EqualTo("Updated.pdf"));
        Assert.That(updatedDoc.Filetype, Is.EqualTo("docx"));
        Assert.That(updatedDoc.ByteSize, Is.EqualTo(1024));
    }

    [Test]
    public void UpdateDocument_WhenDocumentDoesNotExist_ReturnsNotFound()
    {

        var updatedDto = new DocumentDto { Name = "NonExistent.pdf", Filetype = "pdf" };


        var result = _controller.UpdateDocument(999, updatedDto);


        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public void UpdateDocument_WhenIdIsInvalid_ThrowsInvalidIdException()
    {

        var updatedDto = new DocumentDto { Name = "Test.pdf", Filetype = "pdf" };


        Assert.Throws<InvalidIdException>(() => _controller.UpdateDocument(0, updatedDto));
        Assert.Throws<InvalidIdException>(() => _controller.UpdateDocument(-10, updatedDto));
    }

    [Test]
    public void UpdateDocument_UpdatesAllFields_Correctly()
    {

        var originalDoc = new Document
        {
            Id = 1,
            Name = "Old.pdf",
            Filetype = "pdf",
            ByteSize = 100,
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        };
        _context.Documents.Add(originalDoc);
        _context.SaveChanges();

        var now = DateTime.UtcNow;
        var updatedDto = new DocumentDto
        {
            Name = "New.docx",
            Filetype = "docx",
            ByteSize = 500,
            CreatedAt = originalDoc.CreatedAt,
            UpdatedAt = now
        };

        _mockMapper.Setup(m => m.Map<DocumentDto>(It.IsAny<Document>())).Returns(updatedDto);


        _controller.UpdateDocument(1, updatedDto);


        var updatedDoc = _context.Documents.Find(1);
        Assert.That(updatedDoc, Is.Not.Null);
        Assert.That(updatedDoc.Name, Is.EqualTo("New.docx"));
        Assert.That(updatedDoc.Filetype, Is.EqualTo("docx"));
        Assert.That(updatedDoc.ByteSize, Is.EqualTo(500));
        Assert.That(updatedDoc.UpdatedAt, Is.EqualTo(now).Within(TimeSpan.FromSeconds(1)));

    }

    #endregion
}
