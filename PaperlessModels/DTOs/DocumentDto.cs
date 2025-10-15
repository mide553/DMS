namespace PaperlessModels.DTOs
{
    public class DocumentDto
    {
        public string Name { get; set; }
        public string Filetype { get; set; }
        public int ByteSize { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
