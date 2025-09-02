using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MyWebApi.Common;
using MyWebApi.Data;
using MyWebApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace MyWebApi.Middleware;

public class AuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthenticationMiddleware> _logger;

    public AuthenticationMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        ILogger<AuthenticationMiddleware> logger)
    {
        _next = next;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // Skip authentication for public endpoints
            if (IsPublicEndpoint(context.Request.Path))
            {
                await _next(context);
                return;
            }

            var token = ExtractTokenFromRequest(context);
            
            if (string.IsNullOrEmpty(token))
            {
                await HandleUnauthorized(context, "No authentication token provided");
                return;
            }

            var user = await ValidateTokenAndGetUser(context, token);
            
            if (user == null)
            {
                await HandleUnauthorized(context, "Invalid or expired authentication token");
                return;
            }

            // Set user information in HttpContext for use in controllers
            context.Items["User"] = user;
            context.Items["UserId"] = user.Id;
            context.Items["UserEmail"] = user.Email;

            _logger.LogInformation("User {UserId} authenticated successfully for endpoint {Endpoint}", 
                user.Id, context.Request.Path);

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in authentication middleware");
            await HandleUnauthorized(context, "Authentication error occurred");
        }
    }

    private bool IsPublicEndpoint(PathString path)
    {
        var publicEndpoints = new[]
        {
            "/api/v1/auth/login",
            "/api/v1/auth/register",
            "/swagger",
            "/swagger/v1/swagger.json",
            "/swagger/v2/swagger.json"
        };

        return publicEndpoints.Any(endpoint => path.StartsWithSegments(endpoint));
    }

    private string? ExtractTokenFromRequest(HttpContext context)
    {
        // Try to get token from Authorization header
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
        {
            return authHeader.Substring("Bearer ".Length);
        }

        // Try to get token from query string (for backward compatibility)
        var queryToken = context.Request.Query["token"].FirstOrDefault();
        if (!string.IsNullOrEmpty(queryToken))
        {
            return queryToken;
        }

        // Try to get token from custom header
        var customHeaderToken = context.Request.Headers["X-Auth-Token"].FirstOrDefault();
        if (!string.IsNullOrEmpty(customHeaderToken))
        {
            return customHeaderToken;
        }

        return null;
    }

    private async Task<User?> ValidateTokenAndGetUser(HttpContext context, string token)
    {
        try
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var key = Encoding.ASCII.GetBytes(jwtSettings["Secret"]!);

            var tokenHandler = new JwtSecurityTokenHandler();
            
            // Validate token
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                ClockSkew = TimeSpan.Zero,
                ValidateLifetime = true
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

            if (validatedToken is not JwtSecurityToken jwtToken)
            {
                _logger.LogWarning("Invalid JWT token format");
                return null;
            }

            // Check if token is expired
            if (jwtToken.ValidTo < DateTime.UtcNow)
            {
                _logger.LogWarning("JWT token has expired");
                return null;
            }

            // Extract user information from claims
            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
            var emailClaim = principal.FindFirst(ClaimTypes.Email);

            if (userIdClaim == null || emailClaim == null)
            {
                _logger.LogWarning("JWT token missing required claims");
                return null;
            }

            // Get user from database to ensure they still exist and are active
            var dbContext = context.RequestServices.GetRequiredService<ApplicationDbContext>();
            var user = await dbContext.Users
                .FirstOrDefaultAsync(u => u.Id == Guid.Parse(userIdClaim.Value) && u.IsActive);

            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found or inactive", userIdClaim.Value);
                return null;
            }

            return user;
        }
        catch (SecurityTokenExpiredException)
        {
            _logger.LogWarning("JWT token has expired");
            return null;
        }
        catch (SecurityTokenInvalidSignatureException)
        {
            _logger.LogWarning("JWT token has invalid signature");
            return null;
        }
        catch (SecurityTokenInvalidIssuerException)
        {
            _logger.LogWarning("JWT token has invalid issuer");
            return null;
        }
        catch (SecurityTokenInvalidAudienceException)
        {
            _logger.LogWarning("JWT token has invalid audience");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating JWT token");
            return null;
        }
    }

    private async Task HandleUnauthorized(HttpContext context, string message)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";

        var response = ApiResponse<object>.ErrorResponse(message);
        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }
}

// Extension method for easy registration
public static class AuthenticationMiddlewareExtensions
{
    public static IApplicationBuilder UseCustomAuthentication(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AuthenticationMiddleware>();
    }
}
