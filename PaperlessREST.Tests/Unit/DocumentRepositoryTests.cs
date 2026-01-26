using Moq;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaperlessModels.DTOs;
using PaperlessModels.Models;
using PaperlessREST.Data;
using PaperlessREST.Exceptions;
using PaperlessREST.Profiles;
using PaperlessREST.Repositories;
using PaperlessREST.Services;
using System.Text;

namespace PaperlessREST.Tests.Unit;

[TestFixture]
public class DocumentRepositoryMockTests
{
    private IDocumentRepository _repository;
    private ApplicationDBContext _db;
    private IMapper _mapper;
    private Mock<IDocumentStorageService> _storageMock;
    private Mock<IMessageQueueService> _queueMock;
    private Mock<ISearchIndexService> _searchIndexMock;
    private ILogger<DocumentRepository> _logger;

    [SetUp]
    public void Setup()
    {
        // Database
        var options = new DbContextOptionsBuilder<ApplicationDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new ApplicationDBContext(options);

        // AutoMapper (REAL config)
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<DocumentProfile>(); // your real mapping profile
        });
        _mapper = mapperConfig.CreateMapper();

        // Mocks
        _storageMock = new Mock<IDocumentStorageService>();
        _queueMock = new Mock<IMessageQueueService>();
        _searchIndexMock = new Mock<ISearchIndexService>();

        // Logger (real but harmless)
        _logger = new LoggerFactory()
            .CreateLogger<DocumentRepository>();

        // Repository under test
        _repository = new DocumentRepository(
            _db,
            _mapper,
            _storageMock.Object,
            _queueMock.Object,
            _searchIndexMock.Object,
            _logger
        );
    }

    [TearDown]
    public void TearDown()
    {
        _db?.Dispose();
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
    public async Task GetAllDocumentsAsync_NoDocuments_ReturnsEmptyList()
    {
        var result = await _repository.GetAllDocumentsAsync(userId: 1);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(0));
    }

    [Test]
    public async Task GetAllDocumentsAsync_ReturnsOnlyUserDocuments()
    {
        _db.Documents.AddRange(
            new Document { FileName = "a.pdf", UserId = 1 },
            new Document { FileName = "b.pdf", UserId = 2 }
        );
        await _db.SaveChangesAsync();

        var result = await _repository.GetAllDocumentsAsync(1);

        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].FileName, Is.EqualTo("a.pdf"));
    }

    #endregion

    #region GetDocumentById

    [Test]
    public void GetDocumentByIdAsync_NotFound_ThrowsException()
    {
        Assert.ThrowsAsync<NotFoundException>(async () =>
            await _repository.GetDocumentByIdAsync(99, 1));
    }

    [Test]
    public async Task GetDocumentByIdAsync_WrongUser_ThrowsForbidden()
    {
        _db.Documents.Add(new Document { Id = 1, FileName = "Test.pdf", UserId = 2 });
        await _db.SaveChangesAsync();

        Assert.ThrowsAsync<ForbiddenContentException>(async () =>
            await _repository.GetDocumentByIdAsync(1, 1));
    }

    [Test]
    public async Task GetDocumentByIdAsync_ReturnsUsersDocument()
    {
        _db.Documents.AddRange(
            new Document { Id = 1, FileName = "a.pdf", UserId = 1 },
            new Document { Id = 2, FileName = "b.pdf", UserId = 1 }
        );
        await _db.SaveChangesAsync();

        var result = await _repository.GetDocumentByIdAsync(1, 1);

        Assert.That(result.FileName, Is.EqualTo("a.pdf"));
        Assert.That(result.UserId, Is.EqualTo(1));
    }

    #endregion

    #region UploadDocument

    [Test]
    public async Task UploadDocumentAsync_DuplicateFile_ThrowsException()
    {
        _db.Documents.Add(new Document { FileName = "test.pdf", UserId = 1 });
        await _db.SaveChangesAsync();

        var file = CreateFakeFile("test.pdf");

        Assert.ThrowsAsync<FileAlreadyExistsException>(async () =>
            await _repository.UploadDocumentAsync(file, 1));
    }

    [Test]
    public async Task UploadDocumentAsync_StoresMetadata_AndUploadsFile()
    {
        var file = CreateFakeFile();

        var result = await _repository.UploadDocumentAsync(file, userId: 1);

        Assert.That(result.Id, Is.GreaterThan(0));
        Assert.That(_db.Documents.Count(), Is.EqualTo(1));

        _storageMock.Verify(s =>
            s.UploadFileAsync(It.IsAny<string>(), It.IsAny<string>()),
            Times.Once);
    }

    [Test]
    public async Task UploadDocumentAsync_PublishesQueueMessage()
    {
        var file = CreateFakeFile();

        await _repository.UploadDocumentAsync(file, 1);

        _queueMock.Verify(q =>
            q.PublishAsync("ocr_queue", It.Is<Dictionary<string, string>>(d =>
                d.ContainsKey("id") &&
                d.ContainsKey("filename") &&
                d.ContainsKey("userId"))),
            Times.Once);
    }

    #endregion

    #region DeleteDocument

    [Test]
    public async Task DeleteDocumentAsync_WrongDocument_ThrowsNotFound()
    {
        _db.Documents.Add(new Document { Id = 1, FileName = "Test.pdf", UserId = 2 });
        await _db.SaveChangesAsync();

        Assert.ThrowsAsync<NotFoundException>(async () =>
            await _repository.DeleteDocumentAsync(99, 2));
    }

    [Test]
    public async Task DeleteDocumentAsync_WrongUser_ThrowsForbidden()
    {
        _db.Documents.Add(new Document { Id = 1, FileName = "Test.pdf", UserId = 2 });
        await _db.SaveChangesAsync();

        Assert.ThrowsAsync<ForbiddenActionException>(async () =>
            await _repository.DeleteDocumentAsync(1, 1));
    }

    //[Test]
    //public async Task DeleteDocumentAsync_RemovesEverything()
    //{
    //    var doc = new Document { Id = 1, FileName = "a.pdf", UserId = 1 };
    //    _db.Documents.Add(doc);
    //    await _db.SaveChangesAsync();

    //    await _repository.DeleteDocumentAsync(1, 1);

    //    Assert.That(_db.Documents.Count(), Is.EqualTo(0));

    //    _storageMock.Verify(s => s.DeleteFileAsync(It.IsAny<string>()), Times.Once);
    //    _searchIndexMock.Verify(s => s.RemoveIndexAsync(1), Times.Once);
    //}

    #endregion

    #region UpdateDocument

    [Test]
    public async Task UpdateDocumentAsync_WrongDocument_ThrowsNotFound()
    {
        var docModel = new Document { Id = 1, FileName = "Test.pdf", UserId = 2 };
        
        _db.Documents.Add(docModel);
        await _db.SaveChangesAsync();

        var docDto = _mapper.Map<Document, DocumentDto>(docModel);

        Assert.ThrowsAsync<NotFoundException>(async () =>
            await _repository.UpdateDocumentAsync(99, docDto, 2));
    }

    [Test]
    public async Task UpdateDocumentAsync_WrongUser_ThrowsForbidden()
    {
        var docModel = new Document { Id = 1, FileName = "Test.pdf", UserId = 2 };

        _db.Documents.Add(docModel);
        await _db.SaveChangesAsync();

        var docDto = _mapper.Map<Document, DocumentDto>(docModel);

        Assert.ThrowsAsync<ForbiddenActionException>(async () =>
            await _repository.UpdateDocumentAsync(1, docDto, 1));
    }

    [Test]
    public async Task UpdateDocumentAsync_UpdatesDocument()
    {
        var doc = new Document
        {
            Id = 1,
            UserId = 1,
            FileName = "old.pdf",
            ByteSize = 100
        };
        _db.Documents.Add(doc);
        await _db.SaveChangesAsync();

        var dto = new DocumentDto
        {
            FileName = "new.pdf",
            ByteSize = 200
        };

        var result = await _repository.UpdateDocumentAsync(1, dto, 1);

        Assert.That(result.FileName, Is.EqualTo("new.pdf"));
        Assert.That(result.ByteSize, Is.EqualTo(200));
    }

    #endregion
}
