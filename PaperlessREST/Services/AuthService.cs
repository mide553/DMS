using Microsoft.EntityFrameworkCore;
using PaperlessModels.DTOs;
using PaperlessModels.Models;
using PaperlessREST.Data;
using PaperlessREST.Exceptions;
using System.Text;
using System.Security.Cryptography;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

namespace PaperlessREST.Services
{
    public interface IAuthService
    {
        public Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto);
        public Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
    }

    public class AuthService : IAuthService
    {
        private readonly ApplicationDBContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        public AuthService(ApplicationDBContext context, IConfiguration configuration, ILogger<AuthService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
        {
            _logger.LogInformation($"Registering user");

            // Check if username already exists
            bool exists = await _context.Users.AnyAsync(u => u.Username == registerDto.Username);
            if (exists)
            {
                _logger.LogWarning("Username already exists");
                throw new UserAlreadyExistsException(RegisterConflictReason.Username);
            }

            // Check if email already exists
            exists = await _context.Users.AnyAsync(u => u.Email == registerDto.Email);
            if (exists)
            {
                _logger.LogWarning("Email already exists");
                throw new UserAlreadyExistsException(RegisterConflictReason.Email);
            }

            // Hash the password
            string passwordHash = HashPassword(registerDto.Password);

            // Create new user
            var user = new User
            {
                Username = registerDto.Username,
                Email = registerDto.Email,
                PasswordHash = passwordHash,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Generate token
            var token = GenerateJwtToken(user);

            _logger.LogInformation($"New user registered: {user.Username}");
            return new AuthResponseDto
            {
                Token = token,
                Username = user.Username,
                Email = user.Email
            };
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
        {
            // Find user by username
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == loginDto.Username);

            if (user == null)
            {
                throw new UnauthorizedAccessException("Invalid username or password");
            }

            // Verify password
            if (!VerifyPassword(loginDto.Password, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Invalid username or password");
            }

            // Generate token
            var token = GenerateJwtToken(user);

            _logger.LogInformation($"User logged in: {user.Username}");
            return new AuthResponseDto
            {
                Token = token,
                Username = user.Username,
                Email = user.Email
            };
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }

        private string GenerateJwtToken(User user)
        {
            var jwtKey = _configuration["Jwt:Key"] ?? "your-super-secret-key-minimum-32-characters-long-for-security";
            var jwtIssuer = _configuration["Jwt:Issuer"] ?? "PaperlessAPI";
            var jwtAudience = _configuration["Jwt:Audience"] ?? "PaperlessClient";

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private bool VerifyPassword(string password, string passwordHash)
        {
            var hashedInput = HashPassword(password);
            return hashedInput == passwordHash;
        }
    }
}
