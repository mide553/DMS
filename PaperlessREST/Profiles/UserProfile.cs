using AutoMapper;
using PaperlessModels.Models;
using PaperlessModels.DTOs;

namespace PaperlessREST.Profiles
{
    public class UserProfile : Profile
    {
        public UserProfile()
        {
            CreateMap<User, UserResponseDto>(); // Entity -> DTO
        }
    }
}
