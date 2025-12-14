using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        // Foreign key to User
        [Required]
        public int UserId { get; set; }

        // Navigation property
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;
    }
}
