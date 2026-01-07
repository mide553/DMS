using Microsoft.AspNetCore.Mvc;
using PaperlessModels.DTOs;
using PaperlessREST.Exceptions;
using PaperlessREST.Services;

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
        private readonly IUserService _userService;
        private readonly ILogger<UserController> _logger;

        public UserController(IUserService userService, ILogger<UserController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            List<UserResponseDto> users = await _userService.GetAllUsersAsync();

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
                await _userService.DeleteUserAsync(id);

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
