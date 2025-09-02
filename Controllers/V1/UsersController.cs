using Microsoft.AspNetCore.Mvc;
using MyWebApi.Common;
using MyWebApi.DTOs;
using MyWebApi.Interfaces;

namespace MyWebApi.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[AuthorizeRoles("User")] // Custom authorization attribute
public class UsersController : ControllerBase
{
    private readonly IServiceManager _serviceManager;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        IServiceManager serviceManager, 
        ICurrentUserService currentUserService,
        ILogger<UsersController> logger)
    {
        _serviceManager = serviceManager;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<UserDto>>>> GetUsers([FromQuery] FilterParameters parameters)
    {
        var result = await _serviceManager.UserService.GetUsersAsync(parameters);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetUser(Guid id)
    {
        var result = await _serviceManager.UserService.GetUserByIdAsync(id);
        
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserDto>>> CreateUser([FromBody] CreateUserDto createUserDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<UserDto>.ErrorResponse("Invalid model state"));
        }

        var result = await _serviceManager.UserService.CreateUserAsync(createUserDto);
        
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetUser), new { id = result.Data!.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUser(Guid id, [FromBody] UpdateUserDto updateUserDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<UserDto>.ErrorResponse("Invalid model state"));
        }

        var result = await _serviceManager.UserService.UpdateUserAsync(id, updateUserDto);
        
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteUser(Guid id)
    {
        var result = await _serviceManager.UserService.DeleteUserAsync(id);
        
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    [HttpGet("me")]
    public ActionResult<ApiResponse<object>> GetCurrentUser()
    {
        if (!_currentUserService.IsAuthenticated)
        {
            return Unauthorized(ApiResponse<object>.ErrorResponse("User not authenticated"));
        }

        var currentUser = new
        {
            Id = _currentUserService.UserId,
            Email = _currentUserService.UserEmail,
            FirstName = _currentUserService.User?.FirstName,
            LastName = _currentUserService.User?.LastName,
            IsActive = _currentUserService.User?.IsActive
        };

        return Ok(ApiResponse<object>.SuccessResponse(currentUser, "Current user information"));
    }
}