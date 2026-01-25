using Microsoft.AspNetCore.Mvc;
using PaperlessModels.DTOs;
using PaperlessREST.Repositories;
using PaperlessREST.Exceptions;

namespace PaperlessREST.Controllers
{
    public interface IUserController
    {
        public Task<IActionResult> GetAllUsers();
        public Task<IActionResult> DeleteUser(int id);
    }

    [ApiController]
    [Route("api/users")]
    public class UserController : ControllerBase, IUserController
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<UserController> _logger;

        public UserController(IUserRepository userRepository, ILogger<UserController> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            List<UserResponseDto> users = await _userRepository.GetAllUsersAsync();

            return Ok(users);   // 200 Ok
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser([FromRoute] int id)
        {
            if (id < 1)
            {
                _logger.LogWarning($"Invalid user ID: {id}");
                return BadRequest($"Invalid user ID: {id}");    // 400 Bad Request
            }

            try
            {
                await _userRepository.DeleteUserAsync(id);

                return NoContent(); // 204 No Content
            }
            catch (NotFoundException)
            {
                return NotFound();  // 404 Not Found
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error deleting user {id}");
                return StatusCode(500, "An unexpected error occurred"); // 500 Internal Server Error
            }
        }
    }
}
