using Microsoft.AspNetCore.Mvc;
using MyWebApi.Common;
using MyWebApi.Interfaces;

namespace MyWebApi.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/test")]
public class TestController : ControllerBase
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<TestController> _logger;

    public TestController(ICurrentUserService currentUserService, ILogger<TestController> logger)
    {
        _currentUserService = currentUserService;
        _logger = logger;
    }

    [HttpGet("public")]
    public ActionResult<ApiResponse<string>> GetPublicMessage()
    {
        return Ok(ApiResponse<string>.SuccessResponse("This is a public endpoint - no authentication required", "Public message"));
    }

    [HttpGet("authenticated")]
    [AuthorizeRoles("User")]
    public ActionResult<ApiResponse<object>> GetAuthenticatedMessage()
    {
        if (!_currentUserService.IsAuthenticated)
        {
            return Unauthorized(ApiResponse<object>.ErrorResponse("User not authenticated"));
        }

        var response = new
        {
            Message = "This is an authenticated endpoint",
            UserId = _currentUserService.UserId,
            UserEmail = _currentUserService.UserEmail,
            IsAuthenticated = _currentUserService.IsAuthenticated
        };

        return Ok(ApiResponse<object>.SuccessResponse(response, "Authenticated message"));
    }

    [HttpGet("admin")]
    [AuthorizeRoles("Admin")]
    public ActionResult<ApiResponse<object>> GetAdminMessage()
    {
        var response = new
        {
            Message = "This is an admin-only endpoint",
            UserId = _currentUserService.UserId,
            UserEmail = _currentUserService.UserEmail,
            IsAuthenticated = _currentUserService.IsAuthenticated
        };

        return Ok(ApiResponse<object>.SuccessResponse(response, "Admin message"));
    }
}
