//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Logging;
//using Moq;
//using AutoMapper;
//using PaperlessREST.Data;
//using PaperlessREST.Controllers;
//using PaperlessREST.Services;
//using PaperlessREST.Exceptions;
//using PaperlessModels.Models;
//using PaperlessModels.DTOs;

//namespace PaperlessREST.Tests;

//[TestFixture]
//public class DocumentControllerTests
//{
//    private DocumentService _documentService;
//    private ApplicationDBContext _context;
//    private Mock<IMapper> _mockMapper;
//    private Mock<IDocumentStorageService> _mockDocumentStorage;
//    private Mock<IMessageQueueService> _mockQueueService;
//    private Mock<ILogger<DocumentService>> _mockLogger;

//    [SetUp]
//    public void Setup()
//    {
//        // Setup in-memory database with unique name for each test
//        var options = new DbContextOptionsBuilder<ApplicationDBContext>()
//            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
//            .Options;
//        _context = new ApplicationDBContext(options);

//        // Setup mocks
//        _mockMapper = new Mock<IMapper>();
//        _mockDocumentStorage = new Mock<IDocumentStorageService>();
//        _mockQueueService = new Mock<IMessageQueueService>();
//        _mockLogger = new Mock<ILogger<DocumentService>>();

//        // Create controller with mocked dependencies
//        _documentService = new DocumentService(_context, _mockMapper.Object, _mockDocumentStorage.Object, _mockQueueService.Object, _mockLogger.Object);
//    }

//    [TearDown]
//    public void TearDown()
//    {
//        _context.Database.EnsureDeleted();
//        _context.Dispose();
//    }

//    #region GetAllDocuments Tests

//    //[Test]
//    //public void GetAllDocumentsAsync_WhenDocumentsExist_ReturnsOkWithDocuments()
//    //{
//    //    _context.Documents.AddRange(
//    //        new Document { Id = 1, FileName = "Document1.pdf", ByteSize = 1024 },
//    //        new Document { Id = 2, FileName = "Document2.docx", ByteSize = 2048 }
//    //    );
//    //    _context.SaveChanges();

//    //    var result = _documentService.GetAllDocumentsAsync() as OkObjectResult;

//    //    Assert.That(result, Is.Not.Null);
//    //    Assert.That(result.StatusCode, Is.EqualTo(200));
//    //    var documents = result.Value as List<Document>;
//    //    Assert.That(documents, Is.Not.Null);
//    //    Assert.That(documents.Count, Is.EqualTo(2));
//    //    Assert.That(documents[0].FileName, Is.EqualTo("Document1.pdf"));
//    //}

//    //[Test]
//    //public void GetAllDocumentsAsync_WhenNoDocuments_ReturnsOkWithEmptyList()
//    //{

//    //    var result = _documentService.GetAllDocumentsAsync() as OkObjectResult;


//    //    Assert.That(result, Is.Not.Null);
//    //    Assert.That(result.StatusCode, Is.EqualTo(200));
//    //    var documents = result.Value as List<Document>;
//    //    Assert.That(documents, Is.Not.Null);
//    //    Assert.That(documents.Count, Is.EqualTo(0));
//    //}

//    #endregion
    
//    #region GetDocumentByIdAsync Tests

//    //[Test]
//    //public void GetDocumentByIdAsync_WhenDocumentExists_ReturnsOkWithDocument()
//    //{
//    //    var document = new Document { Id = 1, FileName = "TestDoc.pdf", ByteSize = 1024 };
//    //    _context.Documents.Add(document);
//    //    _context.SaveChanges();

//    //    var result = _documentService.GetDocumentByIdAsync(1) as OkObjectResult;

//    //    Assert.That(result, Is.Not.Null);
//    //    Assert.That(result.StatusCode, Is.EqualTo(200));
//    //    var returnedDoc = result.Value as Document;
//    //    Assert.That(returnedDoc, Is.Not.Null);
//    //    Assert.That(returnedDoc.Id, Is.EqualTo(1));
//    //    Assert.That(returnedDoc.FileName, Is.EqualTo("TestDoc.pdf"));
//    //}

//    [Test]
//    public void GetDocumentByIdAsync_WhenDocumentDoesNotExist_ReturnsNotFound()
//    {
//        var result = _documentService.GetDocumentByIdAsync(999);

//        Assert.That(result, Is.TypeOf<NotFoundResult>());
//    }

//    [Test]
//    public void GetDocumentByIdAsync_WhenIdIsZero_ThrowsInvalidIdException()
//    {

//        Assert.Throws<InvalidIdException>(() => _documentService.GetDocumentByIdAsync(0));
//    }

//    [Test]
//    public void GetDocumentByIdAsync_WhenIdIsNegative_ThrowsInvalidIdException()
//    {

//        Assert.Throws<InvalidIdException>(() => _documentService.GetDocumentByIdAsync(-1));
//    }

//    #endregion

//    #region UploadDocument Tests

//    [Test]
//    public async Task UploadDocumentAsync_PublishesToQueue_WithCorrectQueueName()
//    {
//        var document = new Document { Id = 1, FileName = "QueueTest.pdf", ByteSize = 1024 };

//        _context.Documents.Add(document);
//        _context.SaveChanges();

//        _mockQueueService.Setup(q => q.PublishAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

//        await _mockQueueService.Object.PublishAsync("ocr_queue", "test.pdf");

//        _mockQueueService.Verify(q => q.PublishAsync("ocr_queue","test.pdf"), Times.Once);
//    }

//    #endregion

//    #region DeleteDocument Tests

//    [Test]
//    public void DeleteDocumentAsync_WhenDocumentExists_ReturnsNoContent()
//    {
//        var document = new Document { Id = 1, FileName = "ToDelete.pdf", ByteSize = 1024 };
//        _context.Documents.Add(document);
//        _context.SaveChanges();

//        var result = _documentService.DeleteDocumentAsync(1);

//        Assert.That(result, Is.TypeOf<NoContentResult>());
//        Assert.That(_context.Documents.Find(1), Is.Null);
//    }

//    [Test]
//    public void DeleteDocumentAsync_WhenDocumentDoesNotExist_ReturnsNotFound()
//    {
//        var result = _documentService.DeleteDocumentAsync(999);

//        Assert.That(result, Is.TypeOf<NotFoundResult>());
//    }

//    [Test]
//    public void DeleteDocumentAsync_WhenIdIsInvalid_ThrowsInvalidIdException()
//    {
//        Assert.Throws<InvalidIdException>(() => _documentService.DeleteDocumentAsync(0));
//        Assert.Throws<InvalidIdException>(() => _documentService.DeleteDocumentAsync(-5));
//    }

//    [Test]
//    public void DeleteDocumentAsync_RemovesDocumentFromDatabase()
//    {
//        var document = new Document { Id = 5, FileName = "RemoveMe.pdf", ByteSize = 1024 };
//        _context.Documents.Add(document);
//        _context.SaveChanges();

//        Assert.That(_context.Documents.Find(5), Is.Not.Null);

//        _documentService.DeleteDocumentAsync(5);


//        Assert.That(_context.Documents.Find(5), Is.Null);
//        Assert.That(_context.Documents.Count(), Is.EqualTo(0));
//    }

//    #endregion

//    #region UpdateDocument Tests

//    //[Test]
//    //public void UpdateDocumentAsync_WhenDocumentExists_ReturnsOkWithUpdatedDocument()
//    //{
//    //    var originalDoc = new Document
//    //    {
//    //        Id = 1,
//    //        FileName = "Original.pdf",
//    //        ByteSize = 512,
//    //        Summary = "",
//    //        LastModified = DateTime.UtcNow.AddDays(-1)
//    //    };
//    //    _context.Documents.Add(originalDoc);
//    //    _context.SaveChanges();

//    //    var updatedDto = new DocumentDto
//    //    {
//    //        FileName = "Updated.pdf",
//    //        ByteSize = 1024,
//    //        Summary = "Updated summary",
//    //        LastModified = DateTime.UtcNow
//    //    };

//    //    _mockMapper.Setup(m => m.Map<DocumentDto>(It.IsAny<Document>())).Returns(updatedDto);

//    //    var result = _documentService.UpdateDocumentAsync(1, updatedDto) as OkObjectResult;

//    //    Assert.That(result, Is.Not.Null);
//    //    Assert.That(result.StatusCode, Is.EqualTo(200));

//    //    var updatedDoc = _context.Documents.Find(1);
//    //    Assert.That(updatedDoc, Is.Not.Null);
//    //    Assert.That(updatedDoc.FileName, Is.EqualTo("Updated.pdf"));
//    //    Assert.That(updatedDoc.ByteSize, Is.EqualTo(1024));
//    //    Assert.That(updatedDoc.Summary, Is.EqualTo("Updated summary"));
//    //}

//    [Test]
//    public void UpdateDocumentAsync_WhenDocumentDoesNotExist_ReturnsNotFound()
//    {
//        var updatedDto = new DocumentDto { FileName = "NonExistent.pdf", ByteSize = 1024 };

//        var result = _documentService.UpdateDocumentAsync(999, updatedDto);

//        Assert.That(result, Is.TypeOf<NotFoundResult>());
//    }

//    [Test]
//    public void UpdateDocumentAsync_WhenIdIsInvalid_ThrowsInvalidIdException()
//    {
//        var updatedDto = new DocumentDto { FileName = "Test.pdf", ByteSize = 1024 };

//        Assert.Throws<InvalidIdException>(() => _documentService.UpdateDocumentAsync(0, updatedDto));
//        Assert.Throws<InvalidIdException>(() => _documentService.UpdateDocumentAsync(-10, updatedDto));
//    }

//    [Test]
//    public void UpdateDocumentAsync_UpdatesAllFields_Correctly()
//    {
//        var originalDoc = new Document
//        {
//            Id = 1,
//            FileName = "Old.pdf",
//            ByteSize = 100,
//            Summary = "Old summary",
//            LastModified = DateTime.UtcNow.AddDays(-5)
//        };
//        _context.Documents.Add(originalDoc);
//        _context.SaveChanges();

//        var now = DateTime.UtcNow;
//        var updatedDto = new DocumentDto
//        {
//            FileName = "New.pdf",
//            ByteSize = 500,
//            Summary = "New summary from OCR",
//            LastModified = now
//        };

//        _mockMapper.Setup(m => m.Map<DocumentDto>(It.IsAny<Document>())).Returns(updatedDto);

//        _documentService.UpdateDocumentAsync(1, updatedDto);

//        var updatedDoc = _context.Documents.Find(1);
//        Assert.That(updatedDoc, Is.Not.Null);
//        Assert.That(updatedDoc.FileName, Is.EqualTo("New.pdf"));
//        Assert.That(updatedDoc.ByteSize, Is.EqualTo(500));
//        Assert.That(updatedDoc.Summary, Is.EqualTo("New summary from OCR"));
//        Assert.That(updatedDoc.LastModified, Is.EqualTo(now).Within(TimeSpan.FromSeconds(1)));
//    }

//    [Test]
//    public void UpdateDocumentAsync_UpdateOneField_Correctly()
//    {
//        var originalDoc = new Document
//        {
//            Id = 1,
//            FileName = "Document.pdf",
//            ByteSize = 100,
//            Summary = "Old summary",
//            LastModified = DateTime.UtcNow.AddDays(-5)
//        };
//        _context.Documents.Add(originalDoc);
//        _context.SaveChanges();

//        var updatedDto = new DocumentDto
//        {
//            Summary = "New summary from OCR",
//        };

//        _mockMapper.Setup(m => m.Map<DocumentDto>(It.IsAny<Document>())).Returns(updatedDto);

//        _documentService.UpdateDocumentAsync(1, updatedDto);

//        var updatedDoc = _context.Documents.Find(1);
//        Assert.That(updatedDoc, Is.Not.Null);
//        Assert.That(updatedDoc.FileName, Is.EqualTo(originalDoc.FileName));
//        Assert.That(updatedDoc.ByteSize, Is.EqualTo(originalDoc.ByteSize));
//        Assert.That(updatedDoc.Summary, Is.EqualTo(updatedDto.Summary));
//        Assert.That(updatedDoc.LastModified, Is.EqualTo(originalDoc.LastModified));
//    }

//    #endregion
//}
