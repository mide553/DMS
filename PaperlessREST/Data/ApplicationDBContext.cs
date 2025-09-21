using Microsoft.EntityFrameworkCore;
using PaperlessREST.Model;

namespace PaperlessREST.Data
{
    public class ApplicationDBContext : DbContext
    {
        public ApplicationDBContext(DbContextOptions dbContextOptions) 
            : base(dbContextOptions) { }

        public DbSet<Document> Documents { get; set; }
    }
}
