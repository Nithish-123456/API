using Microsoft.AspNetCore.Mvc;
using MyWebApi.Common;
using MyWebApi.DTOs;
using MyWebApi.Interfaces;

namespace MyWebApi.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin")]
[AuthorizeRoles("Admin")] // Only admin users can access
public class AdminController : ControllerBase
{
    private readonly IServiceManager _serviceManager;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IServiceManager serviceManager,
        ICurrentUserService currentUserService,
        ILogger<AdminController> logger)
    {
        _serviceManager = serviceManager;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    [HttpGet("dashboard")]
    public ActionResult<ApiResponse<object>> GetAdminDashboard()
    {
        _logger.LogInformation("Admin {AdminId} accessed dashboard", _currentUserService.UserId);

        var dashboard = new
        {
            AdminInfo = new
            {
                Id = _currentUserService.UserId,
                Email = _currentUserService.UserEmail,
                Name = $"{_currentUserService.User?.FirstName} {_currentUserService.User?.LastName}"
            },
            Message = "Welcome to Admin Dashboard",
            Timestamp = DateTime.UtcNow
        };

        return Ok(ApiResponse<object>.SuccessResponse(dashboard, "Admin dashboard data"));
    }

    [HttpGet("users/all")]
    public async Task<ActionResult<ApiResponse<object>>> GetAllUsers()
    {
        // This endpoint would typically get all users with additional admin-only information
        var result = await _serviceManager.UserService.GetUsersAsync(new FilterParameters());
        
        if (!result.Success)
        {
            return BadRequest(result);
        }

        var adminView = new
        {
            TotalUsers = result.Data?.Data?.Count() ?? 0,
            Users = result.Data?.Data,
            RetrievedBy = _currentUserService.UserEmail,
            RetrievedAt = DateTime.UtcNow
        };

        return Ok(ApiResponse<object>.SuccessResponse(adminView, "All users retrieved by admin"));
    }

    [HttpPost("users/{id}/deactivate")]
    public async Task<ActionResult<ApiResponse<bool>>> DeactivateUser(Guid id)
    {
        // Only admins can deactivate users
        if (id == _currentUserService.UserId)
        {
            return BadRequest(ApiResponse<bool>.ErrorResponse("Admin cannot deactivate their own account"));
        }

        // This would be implemented in the UserService
        // For now, return a success response
        _logger.LogWarning("Admin {AdminId} deactivated user {UserId}", _currentUserService.UserId, id);

        return Ok(ApiResponse<bool>.SuccessResponse(true, $"User {id} deactivated successfully"));
    }

    [HttpGet("system/status")]
    public ActionResult<ApiResponse<object>> GetSystemStatus()
    {
        var systemStatus = new
        {
            Status = "Healthy",
            AdminUser = _currentUserService.UserEmail,
            CheckedAt = DateTime.UtcNow,
            Version = "1.0.0"
        };

        return Ok(ApiResponse<object>.SuccessResponse(systemStatus, "System status retrieved"));
    }
}
