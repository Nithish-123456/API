using Microsoft.AspNetCore.Mvc;
using MyWebApi.Common;
using MyWebApi.DTOs;
using MyWebApi.Interfaces;

namespace MyWebApi.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IServiceManager _serviceManager;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IServiceManager serviceManager, ILogger<AuthController> logger)
    {
        _serviceManager = serviceManager;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login([FromBody] LoginDto loginDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<AuthResponseDto>.ErrorResponse("Invalid model state"));
        }

        var result = await _serviceManager.AuthService.LoginAsync(loginDto);
        
        if (!result.Success)
        {
            return Unauthorized(result);
        }

        return Ok(result);
    }

    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<UserDto>>> Register([FromBody] CreateUserDto createUserDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<UserDto>.ErrorResponse("Invalid model state"));
        }

        var result = await _serviceManager.AuthService.RegisterAsync(createUserDto);
        
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(Register), result);
    }
}