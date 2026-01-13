using Microsoft.EntityFrameworkCore;
using PaperlessModels.Models;

namespace BatchProcessor;

public class BatchDbContext : DbContext
{
    public BatchDbContext(DbContextOptions<BatchDbContext> options) : base(options)
    {
    }

    public DbSet<Document> Documents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<Document>().ToTable("Documents");
    }
}
