using Microsoft.AspNetCore.Http;
using MyWebApi.Common;
using MyWebApi.Interfaces;
using MyWebApi.Models;
using System.Text.Json;

namespace MyWebApi.Middleware;

public class AuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuthorizationMiddleware> _logger;

    public AuthorizationMiddleware(RequestDelegate next, ILogger<AuthorizationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // Check if user is authenticated
            var currentUserService = context.RequestServices.GetRequiredService<ICurrentUserService>();
            
            if (!currentUserService.IsAuthenticated)
            {
                await HandleForbidden(context, "User is not authenticated");
                return;
            }

            // Check if endpoint requires specific roles
            var requiredRoles = GetRequiredRoles(context);
            
            if (requiredRoles.Any() && !HasRequiredRoles(currentUserService.User, requiredRoles))
            {
                _logger.LogWarning("User {UserId} attempted to access {Endpoint} without required roles {RequiredRoles}", 
                    currentUserService.UserId, context.Request.Path, string.Join(", ", requiredRoles));
                
                await HandleForbidden(context, "Insufficient permissions");
                return;
            }

            _logger.LogInformation("User {UserId} authorized for endpoint {Endpoint}", 
                currentUserService.UserId, context.Request.Path);

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in authorization middleware");
            await HandleForbidden(context, "Authorization error occurred");
        }
    }

    private string[] GetRequiredRoles(HttpContext context)
    {
        // You can implement role requirements based on:
        // 1. Route attributes
        // 2. Controller attributes
        // 3. Custom headers
        // 4. Configuration files
        
        // For now, let's check for a custom header
        var requiredRolesHeader = context.Request.Headers["X-Required-Roles"].FirstOrDefault();
        if (!string.IsNullOrEmpty(requiredRolesHeader))
        {
            return requiredRolesHeader.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                   .Select(r => r.Trim())
                                   .ToArray();
        }

        // Check route pattern for role requirements
        var path = context.Request.Path.Value?.ToLower();
        
        if (path?.Contains("/admin/") == true)
        {
            return new[] { "Admin" };
        }
        
        if (path?.Contains("/manager/") == true)
        {
            return new[] { "Admin", "Manager" };
        }

        // Default: no specific roles required
        return Array.Empty<string>();
    }

    private bool HasRequiredRoles(User? user, string[] requiredRoles)
    {
        if (user == null || !requiredRoles.Any())
            return false;

        // For now, we'll implement a simple role check
        // In a real application, you might have a UserRoles table or roles stored in the User model
        
        // Example: Check if user is admin (you can extend this based on your role system)
        if (requiredRoles.Contains("Admin"))
        {
            // Check if user is admin (you can add an IsAdmin property to User model)
            // For now, let's assume admin users have a specific email pattern
            return user.Email.Contains("admin") || user.Email.Contains("Admin");
        }

        if (requiredRoles.Contains("Manager"))
        {
            // Check if user is manager
            return user.Email.Contains("manager") || user.Email.Contains("Manager");
        }

        // If no specific roles are required, allow access
        return true;
    }

    private async Task HandleForbidden(HttpContext context, string message)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
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
public static class AuthorizationMiddlewareExtensions
{
    public static IApplicationBuilder UseCustomAuthorization(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AuthorizationMiddleware>();
    }
}
