using Microsoft.AspNetCore.Mvc;
using PaperlessModels.DTOs;
using PaperlessREST.Repositories;
using PaperlessREST.Exceptions;

namespace PaperlessREST.Controllers
{
    public interface IAuthController
    {
        public Task<IActionResult> Register(RegisterDto registerDto);
        public Task<IActionResult> Login(LoginDto loginDto);
    }

    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase, IAuthController
    {
        private readonly IAuthRepository _authRepository;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthRepository authRepository, ILogger<AuthController> logger)
        {
            _authRepository = authRepository;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            try
            {
                AuthResponseDto auth = await _authRepository.RegisterAsync(registerDto);

                return Ok(auth);    // 200 Ok
            }
            catch (UserAlreadyExistsException ex)
            {
                string error = ex.Reason switch
                {
                    RegisterConflictReason.Username => "Username already exists",
                    RegisterConflictReason.Email => "Email already exists",
                    _ => "User already exists"
                };

                return Conflict(new
                {
                    error = error
                });  // 409 Conflict
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while registering user");
                return StatusCode(500, "An unexpected error occurred"); // 500 Internal Server Error
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                AuthResponseDto auth = await _authRepository.LoginAsync(loginDto);

                return Ok(auth);    // 200 Ok
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new
                {
                    error = "Invalid username or password"
                });  // 401 Unauthorized
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while user login");
                return StatusCode(500, "An unexpected error occurred"); // 500 Internal Server Error
            }
        }
    }
}
