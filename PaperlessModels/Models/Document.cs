using System.ComponentModel.DataAnnotations;

namespace PaperlessModels.Models
{
    public class Document
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Filetype { get; set; }
        public int ByteSize { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
