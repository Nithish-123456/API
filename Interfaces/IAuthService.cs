using MyWebApi.Common;
using MyWebApi.DTOs;

namespace MyWebApi.Interfaces;

public interface IAuthService
{
    Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginDto loginDto);
    Task<ApiResponse<UserDto>> RegisterAsync(CreateUserDto createUserDto);
    string GenerateJwtToken(UserDto user);
}