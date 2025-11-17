using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using PaperlessREST.Services;
using PaperlessREST.Data;
using PaperlessREST.Exceptions;
using PaperlessModels.Models;
using PaperlessModels.DTOs;
using Microsoft.EntityFrameworkCore;

namespace PaperlessREST.Controllers
{
    public interface IDocumentService
    {
        public Task<List<Document>> GetAllDocumentsAsync();
        public Task<DocumentDto> GetDocumentByIdAsync(int id);
        public Task<Document> UploadDocumentAsync(IFormFile file);
        public Task DeleteDocumentAsync(int id);
        public Task<DocumentDto> UpdateDocumentAsync(int id, DocumentDto docDto);
    }

    public class DocumentService : ControllerBase, IDocumentService
    {
        private readonly ApplicationDBContext _context;
        private readonly IMapper _mapper;
        private readonly IDocumentStorageService _documentStorage;
        private readonly IMessageQueueService _queueService;
        private readonly ILogger<DocumentService> _logger;

        public DocumentService(ApplicationDBContext dbContext, IMapper mapper, IDocumentStorageService documentStorage, IMessageQueueService queueService, ILogger<DocumentService> logger)
        {
            _context = dbContext;
            _mapper = mapper;
            _documentStorage = documentStorage;
            _queueService = queueService;
            _logger = logger;
        }

        public async Task<List<Document>> GetAllDocumentsAsync()
        {
            _logger.LogInformation("Fetching all documents");
            List<Document> docs = await _context.Documents.ToListAsync();
            
            return docs;
        }

        public async Task<DocumentDto> GetDocumentByIdAsync(int id)
        {
            _logger.LogInformation($"Fetching document with ID {id}");
            var doc = await _context.Documents.FindAsync(id);

            return _mapper.Map<DocumentDto>(doc);
        }

        public async Task<Document> UploadDocumentAsync(IFormFile file)
        {
            _logger.LogInformation($"Uploading new document");
            
            // Check if filename already exists
            string fileName = file.FileName;
            if (await _documentStorage.FileExistsAsync(fileName))
            {
                _logger.LogWarning($"File {fileName} already exists");
                throw new FileAlreadyExistsException(fileName);
            }

            var tempPath = Path.Combine(Path.GetTempPath(), file.FileName);
            try
            {
                // Save uploaded file temporaryly inside container
                using (var stream = System.IO.File.Create(tempPath))
                {
                    await file.CopyToAsync(stream);
                }

                // Upload document to MinIO
                await _documentStorage.UploadFileAsync(file.FileName, tempPath);

                // Add document to queue
                string queueName = "ocr_queue";
                await _queueService.PublishAsync(queueName, fileName);
                _logger.LogInformation($"Message sent to queue {queueName}");

                // Save metadata to database
                Document docModel = new Document()
                {
                    FileName = fileName,
                    ByteSize = (int) file.Length // TODO: auf long setzen
                };

                _context.Documents.Add(docModel);
                await _context.SaveChangesAsync();
                
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
                System.IO.File.Delete(tempPath);
            }
        }

        public async Task DeleteDocumentAsync(int id)
        {
            _logger.LogInformation($"Deleting document with ID {id}");

            var docModel = await _context.Documents.FirstOrDefaultAsync(x => x.Id == id);

            if (docModel is null)
            {
                _logger.LogWarning($"Document with ID {id} not found");
                throw new DocumentNotFoundException(id);
            }

            try
            {
                await _documentStorage.DeleteFileAsync(docModel.FileName);

                _context.Documents.Remove(docModel);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Document {id} deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to delete document {id}");
                throw new DocumentDeletionException(id, ex);
            }
        }

        public async Task<DocumentDto> UpdateDocumentAsync(int id, DocumentDto docDto)
        {
            _logger.LogInformation($"Updating document with ID {id}");

            var docModel = await _context.Documents.FirstOrDefaultAsync(x => x.Id == id);

            if (docModel is null)
            {
                _logger.LogWarning($"Document with ID {id} not found");
                throw new DocumentNotFoundException(id);
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
                throw new DocumentUpdateException(id, ex);
            }

            // Return updated Document as DTO Object
            return _mapper.Map<DocumentDto>(docModel);
        }
    }
}
