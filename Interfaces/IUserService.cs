using MyWebApi.Common;
using MyWebApi.DTOs;

namespace MyWebApi.Interfaces;

public interface IUserService
{
    Task<ApiResponse<UserDto>> GetUserByIdAsync(Guid id);
    Task<ApiResponse<PagedResult<UserDto>>> GetUsersAsync(FilterParameters parameters);
    Task<ApiResponse<UserDto>> CreateUserAsync(CreateUserDto createUserDto);
    Task<ApiResponse<UserDto>> UpdateUserAsync(Guid id, UpdateUserDto updateUserDto);
    Task<ApiResponse<bool>> DeleteUserAsync(Guid id);
}