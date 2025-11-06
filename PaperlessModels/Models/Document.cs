using System.ComponentModel.DataAnnotations;

namespace PaperlessModels.Models
{
    public class Document
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string FileName { get; set; }
        public int ByteSize { get; set; }
        public string Summary { get; set; } = string.Empty;
        public DateTime LastModified { get; set; } = DateTime.UtcNow;
    }
}
