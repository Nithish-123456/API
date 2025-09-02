using AutoMapper;
using Microsoft.IdentityModel.Tokens;
using MyWebApi.Common;
using MyWebApi.DTOs;
using MyWebApi.Interfaces;
using MyWebApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MyWebApi.Services;

public class AuthService : IAuthService
{
    private readonly IRepositoryManager _repositoryManager;
    private readonly IMapper _mapper;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IRepositoryManager repositoryManager,
        IMapper mapper,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _repositoryManager = repositoryManager;
        _mapper = mapper;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginDto loginDto)
    {
        try
        {
            var user = await _repositoryManager.Users.GetByEmailAsync(loginDto.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            {
                _logger.LogWarning("Failed login attempt for email: {Email}", loginDto.Email);
                return ApiResponse<AuthResponseDto>.ErrorResponse("Invalid email or password");
            }

            if (!user.IsActive)
            {
                _logger.LogWarning("Login attempt for inactive user: {Email}", loginDto.Email);
                return ApiResponse<AuthResponseDto>.ErrorResponse("Account is inactive");
            }

            var userDto = _mapper.Map<UserDto>(user);
            var token = GenerateJwtToken(userDto);

            var authResponse = new AuthResponseDto
            {
                Token = token,
                User = userDto
            };

            _logger.LogInformation("User {UserId} logged in successfully", user.Id);
            return ApiResponse<AuthResponseDto>.SuccessResponse(authResponse, "Login successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for email: {Email}", loginDto.Email);
            return ApiResponse<AuthResponseDto>.ErrorResponse("An error occurred during login");
        }
    }

    public async Task<ApiResponse<UserDto>> RegisterAsync(CreateUserDto createUserDto)
    {
        try
        {
            if (await _repositoryManager.Users.EmailExistsAsync(createUserDto.Email))
            {
                return ApiResponse<UserDto>.ErrorResponse("Email already exists");
            }

            var user = _mapper.Map<User>(createUserDto);
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(createUserDto.Password);

            await _repositoryManager.Users.AddAsync(user);
            await _repositoryManager.SaveAsync();

            var userDto = _mapper.Map<UserDto>(user);
            _logger.LogInformation("User {UserId} registered successfully", user.Id);

            return ApiResponse<UserDto>.SuccessResponse(userDto, "Registration successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for email: {Email}", createUserDto.Email);
            return ApiResponse<UserDto>.ErrorResponse("An error occurred during registration");
        }
    }

    public string GenerateJwtToken(UserDto user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var key = Encoding.ASCII.GetBytes(jwtSettings["Secret"]!);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            new Claim("jti", Guid.NewGuid().ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(int.Parse(jwtSettings["ExpiryInDays"]!)),
            Issuer = jwtSettings["Issuer"],
            Audience = jwtSettings["Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }
}