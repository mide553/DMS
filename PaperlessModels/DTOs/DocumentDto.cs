namespace PaperlessModels.DTOs
{
    public class DocumentDto
    {
        public string FileName { get; set; }
        public int ByteSize { get; set; }
        public string Summary { get; set; }
        public DateTime LastModified { get; set; }
        public int UserId { get; set; }
    }

    public class OwnDocumentDto
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public int ByteSize { get; set; }
        public string Summary { get; set; }
        public DateTime LastModified { get; set; }
    }

    public class IndexedDocument
    {
        public int DocumentId { get; set; }
        public string Content { get; set; } = string.Empty;
        public int UserId { get; set; }
        public DateTime IndexedAt { get; set; } = DateTime.UtcNow;
    }

}
