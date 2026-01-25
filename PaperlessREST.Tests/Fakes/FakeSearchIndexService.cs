using PaperlessREST.Services;
using PaperlessModels.DTOs;

namespace PaperlessREST.Tests.Fakes
{
    public class FakeSearchIndexService : ISearchIndexService
    {
        public Task<List<IndexedDocument>> SearchAsync(string searchText, int userId)
        {
            return Task.FromResult(new List<IndexedDocument>());
        }

        public Task RemoveIndexAsync(int id)
        {
            return Task.CompletedTask;
        }
    }
}
