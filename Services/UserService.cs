using AutoMapper;
using Microsoft.Extensions.Caching.Memory;
using MyWebApi.Common;
using MyWebApi.DTOs;
using MyWebApi.Interfaces;
using MyWebApi.Models;
using System.Linq.Expressions;

namespace MyWebApi.Services;

public class UserService : IUserService
{
    private readonly IRepositoryManager _repositoryManager;
    private readonly IMapper _mapper;
    private readonly IMemoryCache _cache;
    private readonly ILogger<UserService> _logger;
    private const string UserCacheKeyPrefix = "user_";
    private const int CacheExpirationMinutes = 30;

    public UserService(
        IRepositoryManager repositoryManager,
        IMapper mapper,
        IMemoryCache cache,
        ILogger<UserService> logger)
    {
        _repositoryManager = repositoryManager;
        _mapper = mapper;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ApiResponse<UserDto>> GetUserByIdAsync(Guid id)
    {
        try
        {
            var cacheKey = $"{UserCacheKeyPrefix}{id}";
            
            if (_cache.TryGetValue(cacheKey, out UserDto? cachedUser) && cachedUser != null)
            {
                _logger.LogInformation("User {UserId} retrieved from cache", id);
                return ApiResponse<UserDto>.SuccessResponse(cachedUser);
            }

            var user = await _repositoryManager.Users.GetByIdAsync(id);
            if (user == null)
            {
                return ApiResponse<UserDto>.ErrorResponse("User not found");
            }

            var userDto = _mapper.Map<UserDto>(user);
            
            _cache.Set(cacheKey, userDto, TimeSpan.FromMinutes(CacheExpirationMinutes));
            _logger.LogInformation("User {UserId} cached for {Minutes} minutes", id, CacheExpirationMinutes);

            return ApiResponse<UserDto>.SuccessResponse(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user {UserId}", id);
            return ApiResponse<UserDto>.ErrorResponse("An error occurred while retrieving the user");
        }
    }

    public async Task<ApiResponse<PagedResult<UserDto>>> GetUsersAsync(FilterParameters parameters)
    {
        try
        {
            Expression<Func<User, bool>>? filter = null;

            if (!string.IsNullOrEmpty(parameters.SearchTerm))
            {
                var searchTerm = parameters.SearchTerm.ToLower();
                filter = u => u.FirstName.ToLower().Contains(searchTerm) ||
                             u.LastName.ToLower().Contains(searchTerm) ||
                             u.Email.ToLower().Contains(searchTerm);
            }

            var pagedUsers = await _repositoryManager.Users.GetFilteredAsync(parameters, filter);
            var userDtos = _mapper.Map<IEnumerable<UserDto>>(pagedUsers.Data);

            var result = new PagedResult<UserDto>
            {
                Data = userDtos,
                TotalCount = pagedUsers.TotalCount,
                PageNumber = pagedUsers.PageNumber,
                PageSize = pagedUsers.PageSize
            };

            return ApiResponse<PagedResult<UserDto>>.SuccessResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users");
            return ApiResponse<PagedResult<UserDto>>.ErrorResponse("An error occurred while retrieving users");
        }
    }

    public async Task<ApiResponse<UserDto>> CreateUserAsync(CreateUserDto createUserDto)
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
            _logger.LogInformation("User {UserId} created successfully", user.Id);

            return ApiResponse<UserDto>.SuccessResponse(userDto, "User created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return ApiResponse<UserDto>.ErrorResponse("An error occurred while creating the user");
        }
    }

    public async Task<ApiResponse<UserDto>> UpdateUserAsync(Guid id, UpdateUserDto updateUserDto)
    {
        try
        {
            var user = await _repositoryManager.Users.GetByIdAsync(id);
            if (user == null)
            {
                return ApiResponse<UserDto>.ErrorResponse("User not found");
            }

            _mapper.Map(updateUserDto, user);
            await _repositoryManager.Users.UpdateAsync(user);
            await _repositoryManager.SaveAsync();

            // Invalidate cache
            var cacheKey = $"{UserCacheKeyPrefix}{id}";
            _cache.Remove(cacheKey);

            var userDto = _mapper.Map<UserDto>(user);
            _logger.LogInformation("User {UserId} updated successfully", id);

            return ApiResponse<UserDto>.SuccessResponse(userDto, "User updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", id);
            return ApiResponse<UserDto>.ErrorResponse("An error occurred while updating the user");
        }
    }

    public async Task<ApiResponse<bool>> DeleteUserAsync(Guid id)
    {
        try
        {
            if (!await _repositoryManager.Users.ExistsAsync(id))
            {
                return ApiResponse<bool>.ErrorResponse("User not found");
            }

            await _repositoryManager.Users.DeleteAsync(id);
            await _repositoryManager.SaveAsync();

            // Invalidate cache
            var cacheKey = $"{UserCacheKeyPrefix}{id}";
            _cache.Remove(cacheKey);

            _logger.LogInformation("User {UserId} deleted successfully", id);
            return ApiResponse<bool>.SuccessResponse(true, "User deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", id);
            return ApiResponse<bool>.ErrorResponse("An error occurred while deleting the user");
        }
    }
}