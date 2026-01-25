using Microsoft.EntityFrameworkCore;
using AutoMapper;
using PaperlessREST.Data;
using PaperlessREST.Exceptions;
using PaperlessModels.DTOs;
using PaperlessModels.Models;
using PaperlessREST.Services;

namespace PaperlessREST.Repositories
{
    public interface IDocumentRepository
    {
        public Task<List<OwnDocumentDto>> GetAllDocumentsAsync(int userId);
        public Task<DocumentDto> GetDocumentByIdAsync(int id, int userId);
        public Task<List<DocumentDto>> SearchDocumentAsync(string searchText, int userId);
        public Task<Document> UploadDocumentAsync(IFormFile file, int userId);
        public Task DeleteDocumentAsync(int id, int userId);
        public Task<DocumentDto> UpdateDocumentAsync(int id, DocumentDto docDto, int userId);
    }

    public class DocumentRepository : IDocumentRepository
    {
        private readonly ApplicationDBContext _context;
        private readonly IMapper _mapper;
        private readonly IDocumentStorageService _documentStorage;
        private readonly IMessageQueueService _queueService;
        private readonly ISearchIndexService _searchIndexService;
        private readonly ILogger<DocumentRepository> _logger;

        public DocumentRepository(ApplicationDBContext dbContext, IMapper mapper, IDocumentStorageService documentStorage, IMessageQueueService queueService, ISearchIndexService searchIndexService, ILogger<DocumentRepository> logger)
        {
            _context = dbContext;
            _mapper = mapper;
            _documentStorage = documentStorage;
            _queueService = queueService;
            _searchIndexService = searchIndexService;
            _logger = logger;
        }

        public async Task<List<OwnDocumentDto>> GetAllDocumentsAsync(int userId)
        {
            _logger.LogInformation($"Fetching all documents for user {userId}");
            List<Document> docs = await _context.Documents
                .Where(d => d.UserId == userId)
                .ToListAsync();

            if (docs is null || docs.Count == 0)
            {
                _logger.LogWarning($"No document found for user {userId}");
                return new List<OwnDocumentDto>();  // return empty list
            }

            return _mapper.Map<List<OwnDocumentDto>>(docs);
        }

        public async Task<DocumentDto> GetDocumentByIdAsync(int id, int userId)
        {
            _logger.LogInformation($"Fetching document with ID {id} for user {userId}");
            var doc = await _context.Documents
                .FirstOrDefaultAsync(d => d.Id == id);

            if (doc is null)
            {
                _logger.LogWarning($"Document with ID {id} not found");
                throw new NotFoundException("Document", id);
            }

            if (doc.UserId != userId)
            {
                _logger.LogWarning($"User {userId} attempted to see document {id} owned by user {doc.UserId}");
                throw new ForbiddenContentException("Document", id, userId);
            }

            return _mapper.Map<DocumentDto>(doc);
        }

        public async Task<List<DocumentDto>> SearchDocumentAsync(string searchText, int userId)
        {
            _logger.LogInformation($"Searching document for user {userId} containing text {searchText}");

            var result = await _searchIndexService.SearchAsync(searchText, userId);

            if (result == null || result.Count == 0)
            {
                _logger.LogInformation($"No documents of user {userId} found containing text {searchText}");
                return new List<DocumentDto>();     // return empty list
            }

            // Get document objects of result
            List<DocumentDto> docs = new List<DocumentDto>();
            foreach (var doc in result)
            {
                try
                {
                    docs.Add(await GetDocumentByIdAsync(doc.DocumentId, userId));
                }
                catch
                {
                    _logger.LogInformation($"Skipping Document with ID {doc.DocumentId}");
                }
            }

            return docs;
        }

        public async Task<Document> UploadDocumentAsync(IFormFile file, int userId)
        {
            _logger.LogInformation($"Uploading new document for user {userId}");

            // Check if filename already exists for this user
            string fileName = file.FileName;
            if (await _context.Documents.AnyAsync(d => d.FileName == fileName && d.UserId == userId))
            {
                _logger.LogWarning($"File {fileName} already exists for user {userId}");
                throw new FileAlreadyExistsException(fileName);
            }

            string storageFileName = _CreateStorageFilename(userId, fileName);
            var tempPath = Path.Combine(Path.GetTempPath(), storageFileName);
            try
            {
                // Save uploaded file temporaryly inside container
                using (var stream = File.Create(tempPath))
                {
                    await file.CopyToAsync(stream);
                }

                // Upload document to MinIO
                await _documentStorage.UploadFileAsync(storageFileName, tempPath);

                // Save metadata to database
                Document docModel = new Document()
                {
                    FileName = fileName,
                    ByteSize = file.Length,
                    UserId = userId
                };

                _context.Documents.Add(docModel);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Document successfully uploaded (ID: {docModel.Id})");

                // Add document to queue
                var payload = new Dictionary<string, string>
                {
                    { "id", docModel.Id.ToString() },
                    { "filename", storageFileName },
                    { "userId", userId.ToString() }
                };
                await _queueService.PublishAsync("ocr_queue", payload);

                return docModel;  // 201 Created
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to upload document {fileName}");
                throw new DocumentUploadException(fileName, ex);
            }
            finally
            {
                // Delete temp file after upload
                File.Delete(tempPath);
            }
        }

        public async Task DeleteDocumentAsync(int id, int userId)
        {
            _logger.LogInformation($"Deleting document with ID {id} for user {userId}");

            var docModel = await _context.Documents.FirstOrDefaultAsync(x => x.Id == id);

            if (docModel is null)
            {
                _logger.LogWarning($"Document with ID {id} not found");
                throw new NotFoundException("Document", id);
            }

            if (docModel.UserId != userId)
            {
                _logger.LogWarning($"User {userId} attempted to delete document {id} owned by user {docModel.UserId}");
                throw new ForbiddenActionException("Document", id, userId);
            }

            try
            {
                // Remove from search index
                await _searchIndexService.RemoveIndexAsync(docModel.Id);

                // Remove from document storage
                string storageFileName = _CreateStorageFilename(userId, docModel.FileName);
                await _documentStorage.DeleteFileAsync(storageFileName);

                // Remove from database
                _context.Documents.Remove(docModel);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Document {id} deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to delete document {id}");
                throw new DeletionException("Document", id, ex);
            }
        }

        public async Task<DocumentDto> UpdateDocumentAsync(int id, DocumentDto docDto, int userId)
        {
            _logger.LogInformation($"Updating document with ID {id} for user {userId}");

            var docModel = await _context.Documents.FirstOrDefaultAsync(x => x.Id == id);

            if (docModel is null)
            {
                _logger.LogWarning($"Document with ID {id} not found");
                throw new NotFoundException("Document", id);
            }

            if (docModel.UserId != userId)
            {
                _logger.LogWarning($"User {userId} attempted to update document {id} owned by user {docModel.UserId}");
                throw new ForbiddenActionException("Document", id, userId);
            }

            docModel.FileName = docDto.FileName;
            docModel.ByteSize = docDto.ByteSize;
            docModel.Summary = docDto.Summary;
            docModel.LastModified = docDto.LastModified;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Document {id} updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update document {DocumentId}", id);
                throw new UpdateException("Document", id, ex);
            }

            // Return updated Document as DTO Object
            return _mapper.Map<DocumentDto>(docModel);
        }

        private string _CreateStorageFilename(int id, string filename)
        {
            // Generate unique filename for filestorage
            return $"UserID-{id}_{filename}";
        }
    }
}
