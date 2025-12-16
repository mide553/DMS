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
}
