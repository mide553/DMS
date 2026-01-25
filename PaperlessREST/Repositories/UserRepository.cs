using AutoMapper;
using Microsoft.EntityFrameworkCore;
using PaperlessModels.DTOs;
using PaperlessModels.Models;
using PaperlessREST.Data;
using PaperlessREST.Exceptions;

namespace PaperlessREST.Repositories
{
    public interface IUserRepository
    {
        public Task<List<UserResponseDto>> GetAllUsersAsync();
        public Task DeleteUserAsync(int id);
    }

    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDBContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<UserRepository> _logger;

        public UserRepository(ApplicationDBContext dbContext, IMapper mapper, ILogger<UserRepository> logger)
        {
            _context = dbContext;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<List<UserResponseDto>> GetAllUsersAsync()
        {
            _logger.LogInformation("Fetching all users");
            List<User> users = await _context.Users
                //.Include(u => u.Documents)
                .ToListAsync();

            if (users is null || users.Count == 0)
            {
                _logger.LogWarning($"No users found");
                return new List<UserResponseDto>(); // return empty list
            }

            return _mapper.Map<List<UserResponseDto>>(users);
        }

        public async Task DeleteUserAsync(int id)
        {
            _logger.LogInformation($"Deleting user with ID {id}");

            var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == id);

            if (user is null)
            {
                _logger.LogWarning($"User with ID {id} not found");
                throw new NotFoundException("User", id);
            }

            try
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"User {id} deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to delete user {id}");
                throw new DeletionException("User", id, ex);
            }
        }
    }
}
