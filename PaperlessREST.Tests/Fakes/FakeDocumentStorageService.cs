using PaperlessREST.Services;

namespace PaperlessREST.Tests.Fakes
{
    public class FakeDocumentStorageService : IDocumentStorageService
    {
        public List<string> Uploads = new List<string>();

        public Task UploadFileAsync(string documentName, string filePath)
        {
            Uploads.Add(documentName);
            return Task.CompletedTask;
        }

        public Task<bool> FileExistsAsync(string documentName)
        {
            return Task.FromResult(Uploads.Contains(documentName));
        }

        public Task DeleteFileAsync(string documentName)
        {
            Uploads.Remove(documentName);
            return Task.CompletedTask;
        }
    }
}
