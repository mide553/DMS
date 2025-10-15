using AutoMapper;
using PaperlessModels.Models;
using PaperlessModels.DTOs;

namespace PaperlessREST.Profiles
{
    public class DocumentProfile : Profile
    {
        public DocumentProfile()
        {
            CreateMap<Document, DocumentDto>(); // Entity -> DTO
            CreateMap<DocumentDto, Document>(); // DTO -> Entity
        }
    }
}
