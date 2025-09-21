namespace PaperlessREST.Model
{
    public class Document
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Filetype { get; set; }
        public int ByteSize { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
